using System.IO;
using Amazon.Runtime;
using Amazon.S3.Model;

namespace System.Net.S3
{
    public class S3WebResponse : WebResponse
    {
        public S3WebResponse(AmazonWebServiceResponse streamResponse)
        {
            StreamResponse = streamResponse;
        }

        public int StatusCode => (int)StreamResponse.HttpStatusCode;

        public AmazonWebServiceResponse StreamResponse { get; }

        public override long ContentLength => StreamResponse.ContentLength;

        public override string ContentType => "application/xml";

        public override Stream GetResponseStream()
        {
            if (StreamResponse.GetType() == typeof(GetSeekableObjectResponse))
                return ((GetSeekableObjectResponse)StreamResponse).SeekableStream;
            return null;
        }

        public override WebHeaderCollection Headers
        {
            get
            {
                if (StreamResponse.GetType() == typeof(GetObjectResponse))
                    return ((GetObjectResponse)StreamResponse).Headers.ToWebHeaderCollection();
                WebHeaderCollection webHeaderCollection = new WebHeaderCollection();
                webHeaderCollection.Add(HttpRequestHeader.ContentLength, StreamResponse.ContentLength.ToString());
                return webHeaderCollection;
            }
        }

        
    }
}