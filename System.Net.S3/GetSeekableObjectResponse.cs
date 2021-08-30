using System.IO;
using Amazon.Runtime;
using Amazon.S3.Model;

namespace System.Net.S3
{
    public class GetSeekableObjectResponse : StreamResponse
    {
        private readonly SeekableS3Stream seekableS3Stream;

        public GetSeekableObjectResponse(SeekableS3Stream seekableS3Stream)
        {
            this.seekableS3Stream = seekableS3Stream;
            base.HttpStatusCode = seekableS3Stream.HttpStatusCode;
            base.ContentLength = seekableS3Stream.Length;
        }

        public string ContentType => seekableS3Stream.ContentType;

        public Stream SeekableStream => seekableS3Stream;
    }
}