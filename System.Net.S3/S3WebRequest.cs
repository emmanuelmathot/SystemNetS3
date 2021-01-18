using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Logging;

namespace System.Net.S3
{
    public class S3WebRequest : WebRequest
    {

        private static UriParser S3Uri = new GenericUriParser(GenericUriParserOptions.Default);

        private Uri m_Uri;
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
        private Stream m_UploadStream;
        private bool m_GetRequestStreamStarted;
        private AmazonWebServiceRequest m_Request;
        private string m_BucketName;
        private string m_Key;
        private RequestPayer m_RequestPayer;
        private Task m_ResponseTask = null;
        private string m_ContentType = "application/octet-stream";
        private readonly ILogger log;

        private readonly Regex regEx = new Regex(@"^s3://(?'hostOrBucket'[^/]*)(/.*)?$");

        private static readonly S3Credential DefaultS3Credential = new S3Credential(null, null);


        internal S3WebRequest(Uri uri, ILogger log, AmazonS3Client amazonS3, S3BucketsOptions s3BucketsConfiguration = null)
        {
            this.log = log;
            if (!uri.Scheme.Equals("s3", StringComparison.CurrentCultureIgnoreCase))
                throw new ArgumentOutOfRangeException("uri");

            m_Uri = uri;
            m_AmazonS3 = amazonS3;
            this.s3BucketsOptions = s3BucketsConfiguration;
            SetRequestParametersWithUri(uri);
            m_MethodInfo = S3MethodInfo.GetMethodInfo(S3RequestMethods.DownloadObject);
        }

        private void SetRequestParametersWithUri(Uri uri)
        {
            try
            {
                AmazonS3Uri amazonS3Uri = new AmazonS3Uri(uri);
                m_BucketName = amazonS3Uri.Bucket;
                m_Key = amazonS3Uri.Key;
                m_RequestPayer = s3BucketsOptions.ContainsKey(m_BucketName) ? 
                    RequestPayer.FindValue(s3BucketsOptions[BucketName].Payer) : null;
                return;
            }
            catch { }
            Match match = regEx.Match(uri.OriginalString);
            var absolutePath = uri.AbsolutePath;
            if (match.Success)
            {
                try
                {
                    Dns.GetHostEntry(match.Groups["hostOrBucket"].Value);
                }
                catch
                {
                    absolutePath = "/" + uri.Host + absolutePath;
                    ((AmazonS3Config)m_AmazonS3.Config).ForcePathStyle = true;
                }

                var pathParts = absolutePath.Split('/');
                if (pathParts.Length >= 2 && !string.IsNullOrEmpty(pathParts[1]))
                {
                    if (string.IsNullOrEmpty(m_BucketName))
                        m_BucketName = pathParts[1];
                    if (string.IsNullOrEmpty(m_Key))
                        m_Key = string.Join("/", pathParts.Skip(2));
                }
            }
            m_RequestPayer = s3BucketsOptions.ContainsKey(m_BucketName) ? 
                    RequestPayer.FindValue(s3BucketsOptions[BucketName].Payer) : null;
        }

        internal static S3Credential DefaultCredential
        {
            get
            {
                return DefaultS3Credential;
            }
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
                return m_Uri;
            }
        }

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
            S3GetUploadStream asyncResult = null;
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
                BlockingStream uploadStream = new BlockingStream(Convert.ToUInt64(m_ContentLength));
                asyncResult = new S3GetUploadStream(uploadStream, state, callback);
                m_UploadStream = uploadStream;
                Task.Run(() => callback.Invoke(asyncResult));
            }
            catch (Exception exception)
            {
                log.LogError(exception, "error during GetRequestStream");
                throw;
            }

            return asyncResult;
        }

        public override IAsyncResult BeginGetResponse(AsyncCallback callback, object state)
        {
            IAsyncResult asyncResult = null;
            try
            {
                if (m_GetResponseStarted)
                {
                    throw new InvalidOperationException("GetResponse already started");
                }
                m_GetResponseStarted = true;
                CheckError();
                // Let's make the request asynchronously
                SubmitRequest();
                asyncResult = new S3GetResponseStreamResult(m_ResponseTask, state);
                if (callback != null)
                {
                    // Make the callback
                    Task.Run(() => callback.Invoke(asyncResult));
                }
            }
            catch (Exception exception)
            {
                log.LogError(exception, "error during GetResponse");
                throw;
            }

            return asyncResult;
        }

        public override Stream EndGetRequestStream(IAsyncResult asyncResult)
        {
            Stream requestStream = null;

            try
            {
                // parameter validation
                if (asyncResult == null)
                {
                    throw new ArgumentNullException("asyncResult");
                }

                S3GetUploadStream s3AsyncResult = asyncResult as S3GetUploadStream;
                if (s3AsyncResult == null)
                {
                    throw new ArgumentException("asyncResult not an S3 async result");
                }

                CheckError();
                requestStream = m_UploadStream;

            }
            catch (Exception exception)
            {
                log.LogError(exception, "error during EndGetRequestStream");
                throw;
            }

            return requestStream;
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
                // Let's complete the request
                CompleteRequest();
                CheckError();
            }

            catch (Exception exception)
            {
                log.LogError(exception, "error during EndGetResponse");
                throw;
            }

            return m_S3WebResponse;
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



        private void CompleteRequest()
        {
            m_ResponseTask.GetAwaiter().GetResult();
            if (m_S3WebResponse != null) return;
            switch (m_MethodInfo.Operation)
            {
                case S3Operation.GetObject:
                    m_S3WebResponse = new S3ObjectWebResponse<GetObjectResponse>(GetStreamResponse<GetObjectResponse>());
                    break;
                case S3Operation.ListObject:
                    m_S3WebResponse = new S3ObjectWebResponse<ListObjectsResponse>(GetStreamResponse<ListObjectsResponse>());
                    break;
                case S3Operation.ListBuckets:
                    m_S3WebResponse = new S3ObjectWebResponse<ListBucketsResponse>(GetStreamResponse<ListBucketsResponse>());
                    break;
                case S3Operation.PutBucket:
                    m_S3WebResponse = new S3ObjectWebResponse<PutBucketResponse>(GetStreamResponse<PutBucketResponse>());
                    break;
                case S3Operation.PutObject:
                    m_S3WebResponse = new S3ObjectWebResponse<PutObjectResponse>(GetStreamResponse<PutObjectResponse>());
                    break;
                default:
                    throw new NotSupportedException("S3 operation " + m_MethodInfo.Operation + " is not supported");
            }
        }



        private Task SubmitRequest()
        {
            switch (m_MethodInfo.Operation)
            {
                case S3Operation.GetObject:
                    GetObjectRequest gorequest = CreateGetObjectRequest();
                    m_ResponseTask = m_AmazonS3.GetObjectAsync(gorequest);
                    break;
                case S3Operation.ListObject:
                    ListObjectsRequest lorequest = CreateListObjectsRequest();
                    m_ResponseTask = m_AmazonS3.ListObjectsAsync(lorequest);
                    break;
                case S3Operation.ListBuckets:
                    ListBucketsRequest lbrequest = CreateListBucketsRequest();
                    m_ResponseTask = m_AmazonS3.ListBucketsAsync(lbrequest);
                    break;
                case S3Operation.PutBucket:
                    PutBucketRequest pbrequest = CreatePutBucketsRequest();
                    m_ResponseTask = m_AmazonS3.PutBucketAsync(pbrequest);
                    break;
                case S3Operation.PutObject:
                    PutObjectRequest porequest = CreatePutObjectRequest();
                    m_ResponseTask = m_AmazonS3.PutObjectAsync(porequest);
                    break;
                default:
                    throw new NotSupportedException("S3 operation " + m_MethodInfo.Operation + " is not supported");
            }
            return m_ResponseTask;
        }

        private PutObjectRequest CreatePutObjectRequest()
        {
            if (string.IsNullOrEmpty(m_BucketName))
                throw new ArgumentException("Missing bucket name for PutObject operation");

            if (string.IsNullOrEmpty(m_Key))
                throw new ArgumentException("Missing key for PutObject operation");

            return new PutObjectRequest
            {
                BucketName = m_BucketName,
                Key = m_Key,
                InputStream = m_UploadStream,
                ContentType = m_ContentType,
                // UseChunkEncoding = false,
                // AutoResetStreamPosition = false,
                // AutoCloseStream = false,
                Headers = { ContentLength = m_ContentLength },
            };
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

        private T GetStreamResponse<T>() where T : AmazonWebServiceResponse
        {
            if (m_ResponseTask == null) return null;
            var tcs = new TaskCompletionSource<T>();

            m_ResponseTask.ContinueWith(t => tcs.SetResult(((Task<T>)t).Result), TaskContinuationOptions.OnlyOnRanToCompletion);
            m_ResponseTask.ContinueWith(t => tcs.SetException(t.Exception.InnerExceptions), TaskContinuationOptions.OnlyOnFaulted);
            m_ResponseTask.ContinueWith(t => tcs.SetCanceled(), TaskContinuationOptions.OnlyOnCanceled);

            return tcs.Task.Result;
        }


    }
}
