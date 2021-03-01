using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;
using Microsoft.Extensions.Logging;

namespace System.Net.S3
{
    public class S3WebRequest : WebRequest
    {

        private static UriParser S3Uri = new GenericUriParser(GenericUriParserOptions.Default);

        private S3MethodInfo m_MethodInfo;
        private string m_MoveTo;
        private int m_Timeout;
        private long m_ContentLength;
        private string m_ConnectionGroupName;
        private Exception m_Exception;

        private AmazonS3Client m_AmazonS3 = null;
        private readonly S3BucketsOptions s3BucketsOptions;
        private S3WebResponse m_S3WebResponse;
        private bool m_GetResponseStarted;
        private DateTime m_StartTime;
        private object m_SyncObject;
        private bool m_Async;
        private Task<BlockingStream> m_UploadStream = Task.FromResult<BlockingStream>(null);
        private bool m_GetRequestStreamStarted;
        private AmazonWebServiceRequest m_Request;
        public string m_BucketName;
        public string m_Key;
        private RequestPayer m_RequestPayer;
        private Task<AmazonWebServiceResponse> m_ResponseTask = null;
        private string m_ContentType = "application/octet-stream";
        private readonly ILogger log;

        private static readonly S3Credential DefaultS3Credential = new S3Credential(null, null);


        internal S3WebRequest(Uri uri, ILogger log, AmazonS3Client amazonS3, S3BucketsOptions s3BucketsConfiguration = null)
        {
            this.log = log;
            if (!uri.Scheme.Equals("s3", StringComparison.CurrentCultureIgnoreCase))
                throw new ArgumentOutOfRangeException("uri");

            m_AmazonS3 = amazonS3;
            this.s3BucketsOptions = s3BucketsConfiguration;
            SetRequestParametersWithUri(uri);
            m_MethodInfo = S3MethodInfo.GetMethodInfo(S3RequestMethods.DownloadObject);
        }

        private void SetRequestParametersWithUri(Uri uri)
        {
            m_BucketName = S3UriParser.GetBucketName(uri);
            m_Key = S3UriParser.GetKey(uri);
            if (s3BucketsOptions != null && s3BucketsOptions.ContainsKey(m_BucketName))
                m_RequestPayer = RequestPayer.FindValue(s3BucketsOptions[BucketName].Payer);
        }

        internal static S3Credential DefaultCredential
        {
            get
            {
                return DefaultS3Credential;
            }
        }

        public S3WebRequest Clone(string key)
        {
            var clone = new S3WebRequest(RequestUri, log, S3Client, s3BucketsOptions);
            clone.Key = key;
            return clone;
        }


        /// <summary>
        /// <para>
        /// Selects upload or download of files. WebRequestMethods.Ftp.DownloadFile is default.
        /// Not allowed to be changed once request is started.
        /// </para>
        /// </summary>
        public override string Method
        {
            get
            {
                return m_MethodInfo.Method;
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("value");
                }
                if (InUse)
                {
                    throw new InvalidOperationException("Cannot change method while request in use");
                }
                try
                {
                    m_MethodInfo = S3MethodInfo.GetMethodInfo(value);
                }
                catch (ArgumentException)
                {
                    throw new ArgumentException(string.Format("invalid method {0} for S3", value));
                }
            }
        }

        public bool InUse => m_ResponseTask != null;

        /// <summary>
        /// <para>
        /// Sets the target name for the S3RequestMethods.Move operation
        /// Not allowed to be changed once request is started.
        /// </para>
        /// </summary>
        public string MoveTo
        {
            get
            {
                return m_MoveTo;
            }
            set
            {
                if (InUse)
                {
                    throw new InvalidOperationException("Cannot change target while request in use");
                }

                if (String.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("target for move must not be null or empty");
                }

                m_MoveTo = value;
            }
        }

        /// <summary>
        /// <para>Used for clear text authentication with FTP server</para>
        /// </summary>
        public override ICredentials Credentials
        {
            get
            {
                return null;
            }
            set
            {
            }
        }

        /// <summary>
        /// <para>Gets the Uri used to make the request</para>
        /// </summary>
        public override Uri RequestUri
        {
            get
            {
                if (((AmazonS3Config)m_AmazonS3.Config).ForcePathStyle)
                    return new Uri(string.Format("s3://{0}/{1}", m_BucketName, m_Key));
                return new Uri(string.Format("s3://{0}/{1}", m_BucketName, m_Key));
            }
        }

        public AmazonS3Client S3Client => m_AmazonS3;

        /// <summary>
        /// <para>Timeout of the blocking calls such as GetResponse and GetRequestStream (default 100 secs)</para>
        /// </summary>
        public override int Timeout
        {
            get
            {
                return m_Timeout;
            }
            set
            {
                if (InUse)
                {
                    throw new InvalidOperationException("Cannot change uri while request in use");
                }
                if (value < 0 && value != System.Threading.Timeout.Infinite)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                if (m_Timeout != value)
                {
                    m_Timeout = value;
                }
            }
        }

        /// <summary>
        /// <para>Gets or sets the bucket name of the S3</para>
        /// </summary>
        public string BucketName
        {
            get
            {
                return m_BucketName;
            }
            set
            {
                m_BucketName = value;
            }
        }

        public string Key
        {
            get
            {
                return m_Key;
            }
            set
            {
                m_Key = value;
            }
        }

        /// <summary>
        /// <para>Gets or sets the data size of to-be uploaded data</para>
        /// </summary>
        public override long ContentLength
        {
            get
            {
                return m_ContentLength;
            }
            set
            {
                m_ContentLength = value;
            }
        }

        /// <summary>
        /// <para>Gets or sets the data type of to-be uploaded data</para>
        /// </summary>
        public override string ContentType
        {
            get
            {
                return m_ContentType;
            }
            set
            {
                m_ContentType = value;
            }
        }

        /// <devdoc>
        /// <para>Allows private ConnectionPool(s) to be used</para>
        /// </devdoc>
        public override string ConnectionGroupName
        {
            get
            {
                return m_ConnectionGroupName;
            }
            set
            {
                if (InUse)
                {
                    throw new InvalidOperationException("Cannot change connection group name while request in use");
                }
                m_ConnectionGroupName = value;
            }
        }

        /// <summary>
        ///    <para>Opposite of SetException, rethrows the exception</para>
        /// </summary>
        private void CheckError()
        {
            if (m_Exception != null)
            {
                throw m_Exception;
            }
        }

        private static int GetStatusCode(S3WebResponse s3WebResponse)
        {
            int result = -1;

            if (s3WebResponse != null)
            {
                try
                {
                    result = (int)s3WebResponse.StatusCode;
                }
                catch (ObjectDisposedException)
                {
                    // ObjectDisposedException is expected here in the following sequuence: ftpWebRequest.GetResponse().Dispose() -> ftpWebRequest.GetResponse()
                    // on the second call to GetResponse() we cannot determine the statusCode.
                }
            }

            return result;
        }

        public override IAsyncResult BeginGetRequestStream(AsyncCallback callback, object state)
        {
            try
            {
                if (m_GetRequestStreamStarted)
                {
                    throw new InvalidOperationException("GetRequestStream already started");
                }
                m_GetRequestStreamStarted = true;
                if (!m_MethodInfo.IsUpload)
                {
                    throw new ProtocolViolationException("GetRequestStream is not possible with method " + m_MethodInfo.Method);
                }
                CheckError();
                m_UploadStream = Task.Run<BlockingStream>(() => new BlockingStream(Convert.ToUInt64(m_ContentLength)));
                var tcs = new TaskCompletionSource<BlockingStream>(state);
                m_UploadStream.ContinueWith(t =>
                                  {
                                      if (t.IsFaulted)
                                          tcs.TrySetException(t.Exception.InnerExceptions);
                                      else if (t.IsCanceled)
                                          tcs.TrySetCanceled();
                                      else
                                          tcs.TrySetResult(t.Result);

                                      if (callback != null)
                                          callback(tcs.Task);
                                  }, TaskScheduler.Default);
                // asyncResult = new S3GetUploadStream(uploadStream, state, callback);
                // callback.Invoke(asyncResult);
                // m_UploadStream = uploadStream;
                return tcs.Task;

            }
            catch (Exception exception)
            {
                log.LogError(exception, "error during GetRequestStream");
                throw;
            }
        }

        public override IAsyncResult BeginGetResponse(AsyncCallback callback, object state)
        {
            try
            {
                if (m_GetResponseStarted)
                {
                    throw new InvalidOperationException("GetResponse already started");
                }
                m_GetResponseStarted = true;
                CheckError();
                m_ResponseTask = m_UploadStream.ContinueWith<AmazonWebServiceResponse>(t =>
                {
                    // Let's make the request asynchronously
                    return SubmitRequest();
                });
                var tcs = new TaskCompletionSource<AmazonWebServiceResponse>(state);
                m_ResponseTask.ContinueWith(t =>
                                  {
                                      if (t.IsFaulted)
                                          tcs.TrySetException(t.Exception.InnerExceptions);
                                      else if (t.IsCanceled)
                                          tcs.TrySetCanceled();
                                      else
                                          tcs.TrySetResult(t.Result);

                                      if (callback != null)
                                          callback(tcs.Task);
                                  }, TaskScheduler.Default);
                return tcs.Task;
            }
            catch (Exception exception)
            {
                log.LogError(exception, "error during GetResponse");
                throw;
            }
        }

        public override Stream EndGetRequestStream(IAsyncResult asyncResult)
        {
            try
            {
                // parameter validation
                if (asyncResult == null)
                {
                    throw new ArgumentNullException("asyncResult");
                }

                Task<BlockingStream> s3AsyncResult = asyncResult as Task<BlockingStream>;
                if (s3AsyncResult == null)
                {
                    throw new ArgumentException("asyncResult not an S3 async result");
                }
                return s3AsyncResult.Result;
            }
            catch (Exception exception)
            {
                log.LogError(exception, "error during EndGetRequestStream");
                throw;
            }

        }

        public override WebResponse EndGetResponse(IAsyncResult asyncResult)
        {
            try
            {
                // parameter validation
                if (asyncResult == null)
                {
                    throw new ArgumentNullException("asyncResult");
                }
                Task<AmazonWebServiceResponse> s3AsyncResult = asyncResult as Task<AmazonWebServiceResponse>;
                if (s3AsyncResult == null)
                {
                    throw new ArgumentException("asyncResult not an S3 async result");
                }
                CheckError();

                return CompleteRequest(s3AsyncResult.Result);
            }

            catch (Exception exception)
            {
                log.LogError(exception, "error during EndGetResponse");
                throw;
            }
        }

        public override WebHeaderCollection Headers
        {
            get
            {
                return new WebHeaderCollection();
            }
            set
            {

            }
        }

        public override IWebProxy Proxy
        {
            get
            {
                return null;
            }
            set
            {

            }
        }



        public override Stream GetRequestStream()
        {
            var result = BeginGetRequestStream(null, null);
            return EndGetRequestStream(result);
        }

        public override Task<Stream> GetRequestStreamAsync()
        {
            return Task.Factory.FromAsync(BeginGetRequestStream, EndGetRequestStream, null);
        }

        public override WebResponse GetResponse()
        {
            var result = BeginGetResponse(null, null);
            return EndGetResponse(result);
        }

        public override Task<WebResponse> GetResponseAsync()
        {
            return Task.Factory.FromAsync(BeginGetResponse, EndGetResponse, null);
        }

        private S3WebResponse CompleteRequest(AmazonWebServiceResponse response)
        {
            switch (m_MethodInfo.Operation)
            {
                case S3Operation.GetObject:
                    return new S3ObjectWebResponse<GetObjectResponse>(response);
                case S3Operation.ListObject:
                    return new S3ObjectWebResponse<ListObjectsResponse>(response);
                case S3Operation.ListBuckets:
                    return new S3ObjectWebResponse<ListBucketsResponse>(response);
                case S3Operation.PutBucket:
                    return new S3ObjectWebResponse<PutBucketResponse>(response);
                case S3Operation.PutObject:
                    return new S3ObjectWebResponse<PutObjectResponse>(response);
                default:
                    throw new NotSupportedException("S3 operation " + m_MethodInfo.Operation + " is not supported");
            }
        }



        private AmazonWebServiceResponse SubmitRequest()
        {
            switch (m_MethodInfo.Operation)
            {
                case S3Operation.GetObject:
                    GetObjectRequest gorequest = CreateGetObjectRequest();
                    return m_AmazonS3.GetObjectAsync(gorequest).Result;
                case S3Operation.ListObject:
                    ListObjectsRequest lorequest = CreateListObjectsRequest();
                    return m_AmazonS3.ListObjectsAsync(lorequest).Result;
                case S3Operation.ListBuckets:
                    ListBucketsRequest lbrequest = CreateListBucketsRequest();
                    return m_AmazonS3.ListBucketsAsync(lbrequest).Result;
                case S3Operation.PutBucket:
                    PutBucketRequest pbrequest = CreatePutBucketsRequest();
                    return m_AmazonS3.PutBucketAsync(pbrequest).Result;
                case S3Operation.PutObject:
                    TransferUtilityUploadRequest porequest = CreatePutObjectRequest();
                    new TransferUtility(m_AmazonS3).UploadAsync(porequest).Wait();
                    return new PutObjectResponse()
                    {
                        HttpStatusCode = HttpStatusCode.OK
                    };
                // return m_AmazonS3.PutObjectAsync(porequest).Result;
                default:
                    throw new NotSupportedException("S3 operation " + m_MethodInfo.Operation + " is not supported");
            }
        }

        private TransferUtilityUploadRequest CreatePutObjectRequest()
        {
            if (string.IsNullOrEmpty(m_BucketName))
                throw new ArgumentException("Missing bucket name for PutObject operation");

            if (string.IsNullOrEmpty(m_Key))
                throw new ArgumentException("Missing key for PutObject operation");

            return new TransferUtilityUploadRequest
            {
                BucketName = m_BucketName,
                InputStream = m_UploadStream.Result,
                Key = m_Key,
                Headers = { ContentLength = m_ContentLength },
                ContentType = m_ContentType,
                CannedACL = S3CannedACL.BucketOwnerFullControl,
            };
            // return new PutObjectRequest
            // {
            //     BucketName = m_BucketName,
            //     Key = m_Key,
            //     InputStream = m_UploadStream.Result,
            //     ContentType = m_ContentType,
            //     UseChunkEncoding = false,
            //     AutoResetStreamPosition = false,
            //     AutoCloseStream = false,
            //     Headers = { ContentLength = m_ContentLength },
            // };
        }

        private PutBucketRequest CreatePutBucketsRequest()
        {
            if (string.IsNullOrEmpty(m_BucketName))
                throw new ArgumentException("Missing bucket name for PutBucket operation");

            return new PutBucketRequest
            {
                BucketName = m_BucketName,
            };
        }

        private ListObjectsRequest CreateListObjectsRequest()
        {
            return new ListObjectsRequest
            {
                BucketName = m_BucketName,
                Prefix = m_Key,
                RequestPayer = m_RequestPayer
            };
        }

        private GetObjectRequest CreateGetObjectRequest()
        {
            return new GetObjectRequest
            {
                BucketName = m_BucketName,
                Key = m_Key,
                RequestPayer = m_RequestPayer
            };
        }

        private ListBucketsRequest CreateListBucketsRequest()
        {
            return new ListBucketsRequest
            {
            };
        }

        // private async Task<AmazonWebServiceResponse> GetStreamResponse<T>(Task<T> task) where T : AmazonWebServiceResponse
        // {
        //     if (task == null) return null;
        //     var tcs = new TaskCompletionSource<AmazonWebServiceResponse>();

        //     tcs.SetResult((T)await task);

        //     return await tcs.Task;
        // }


    }
}
