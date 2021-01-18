using Amazon.S3.Model;
using System.Net;
using System.Net.Http;

namespace System.Net.S3
{
    public static class S3Extensions
    {
        public static WebHeaderCollection ToWebHeaderCollection(this HeadersCollection headersCollection)
        {
            WebHeaderCollection webHeaderCollection = new WebHeaderCollection();
            foreach(var headerKey in headersCollection.Keys){
                webHeaderCollection.Add(headerKey, headersCollection[headerKey]);
            }
            return webHeaderCollection;
        }
    }
}