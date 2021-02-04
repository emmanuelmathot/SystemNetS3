using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3.Model;

namespace System.Net.S3
{
    public class S3ObjectWebResponse<T> : S3WebResponse where T : AmazonWebServiceResponse
    {

        public S3ObjectWebResponse(T streamResponse) : base(streamResponse)
        {
        }

        public S3ObjectWebResponse(AmazonWebServiceResponse streamResponse) : this((T)streamResponse)
        {
        }

        public T GetObject() => (T)StreamResponse;

        public override string ContentType
        {
            get
            {
                if (StreamResponse.GetType() == typeof(GetObjectResponse))
                    return ((GetObjectResponse)StreamResponse).Headers["Content-Type"];
                return base.ContentType;
            }
        }
    }
}