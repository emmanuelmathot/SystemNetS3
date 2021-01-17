using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.S3.Model;
using System.Net.S3.Tests.XUnit;
using Xunit;

namespace System.Net.S3.Tests
{
    [Collection(nameof(S3TestCollection))]
    [TestCaseOrderer("System.Net.S3.Tests.XUnit.PriorityOrderer", "System.Net.S3.Tests")]
    public class UploadScenarioTests : BaseTests
    {
        // 0. List no bucket
        [Fact, TestPriority(0)]
        public void S3ListBuckets()
        {
            System.Net.S3.S3WebRequest s3WebRequest = (System.Net.S3.S3WebRequest)WebRequest.Create("s3://localhost");
            s3WebRequest.Method = "LSB";
            System.Net.S3.S3ObjectWebResponse<ListBucketsResponse> s3WebResponse = (System.Net.S3.S3ObjectWebResponse<ListBucketsResponse>)s3WebRequest.GetResponse();

            Assert.Equal(200, s3WebResponse.StatusCode);

            Assert.Equal(0, s3WebResponse.GetObject().Buckets.Count);
        }

        // 1. Create a bucket
        [Fact, TestPriority(1)]
        public async Task S3CreateBucket()
        {
            System.Net.S3.S3WebRequest s3WebRequest = (System.Net.S3.S3WebRequest)WebRequest.Create("s3://bucket1");
            s3WebRequest.Method = "MKB";
            Assert.Equal("bucket1", s3WebRequest.BucketName);
            System.Net.S3.S3ObjectWebResponse<PutBucketResponse> s3WebResponse =
                (System.Net.S3.S3ObjectWebResponse<PutBucketResponse>)(await s3WebRequest.GetResponseAsync());

            Assert.Equal(200, s3WebResponse.StatusCode);
        }

        // 2. List new bucket
        [Fact, TestPriority(2)]
        public void S3ListBuckets2()
        {
            System.Net.S3.S3WebRequest s3WebRequest = (System.Net.S3.S3WebRequest)WebRequest.Create("s3://localhost");
            s3WebRequest.Method = "LSB";
            System.Net.S3.S3ObjectWebResponse<ListBucketsResponse> s3WebResponse = (System.Net.S3.S3ObjectWebResponse<ListBucketsResponse>)s3WebRequest.GetResponse();

            Assert.Equal(200, s3WebResponse.StatusCode);

            Assert.Equal(1, s3WebResponse.GetObject().Buckets.Count);
            Assert.Equal("bucket1", s3WebResponse.GetObject().Buckets.First().BucketName);
        }

        // 3. Upload Test file
        [Fact, TestPriority(3)]
        public async Task S3UploadFile()
        {
            System.Net.S3.S3WebRequest s3WebRequest = (System.Net.S3.S3WebRequest)WebRequest.Create("s3://bucket1/testfile1.txt");
            s3WebRequest.Method = "POST";

            Stream uploadStream = await s3WebRequest.GetRequestStreamAsync();

            Helpers.RunContentStreamGenerator(128, uploadStream);

            s3WebRequest.ContentLength = 128 * 1024 * 1024;

            System.Net.S3.S3WebResponse s3WebResponse = (System.Net.S3.S3WebResponse)await s3WebRequest.GetResponseAsync();

            Assert.Equal(200, s3WebResponse.StatusCode);
        }

        // 4. Upload Test file
        [Fact, TestPriority(4)]
        public async Task S3ListUploadedFile()
        {
            System.Net.S3.S3WebRequest s3WebRequest = (System.Net.S3.S3WebRequest)WebRequest.Create("s3://bucket1/testfile1.txt");
            s3WebRequest.Method = "LS";

            System.Net.S3.S3ObjectWebResponse<ListObjectsResponse> s3WebResponse =
                (System.Net.S3.S3ObjectWebResponse<ListObjectsResponse>)await s3WebRequest.GetResponseAsync();

            Assert.Equal(200, s3WebResponse.StatusCode);

            Assert.Equal(1, s3WebResponse.GetObject().S3Objects.Count);
            Assert.Equal(128 * 1024 * 1024, s3WebResponse.GetObject().S3Objects.First().Size);

        }

        // 5. Download Test file
        [Fact, TestPriority(5)]
        public async Task S3DownloadFile()
        {
            System.Net.S3.S3WebRequest s3WebRequest = (System.Net.S3.S3WebRequest)WebRequest.Create("s3://bucket1/testfile1.txt");
            s3WebRequest.Method = "GET";

            long i = 0;

            System.Net.S3.S3WebResponse s3WebResponse = (System.Net.S3.S3WebResponse)await s3WebRequest.GetResponseAsync();
            Assert.Equal(200, s3WebResponse.StatusCode);
            Assert.Equal("application/octet-stream", s3WebResponse.ContentType);
            Assert.Equal(128 * 1024 * 1024, s3WebResponse.ContentLength);

            using (Stream s3WebResponseStream = s3WebResponse.GetResponseStream())
            {
                int j = 0;
                do
                {
                    j = s3WebResponseStream.Read(new byte[1024], 0, 1024);
                    i += j;
                }
                while (j > 0);
            }

            Assert.Equal(128 * 1024 * 1024, i);

        }
    }
}
