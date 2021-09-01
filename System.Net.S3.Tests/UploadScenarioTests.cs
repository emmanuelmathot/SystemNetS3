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
        // 0. Delete bucket
        // [Fact, TestPriority(0)]
        // public void S3DeleteBucket()
        // {
        //     System.Net.S3.S3WebRequest s3WebRequest = (System.Net.S3.S3WebRequest)WebRequest.Create("s3://bucket1");
        //     s3WebRequest.Method = S3RequestMethods.RemoveBucket;
        //     System.Net.S3.S3ObjectWebResponse<DeleteBucketResponse> s3WebResponse = (System.Net.S3.S3ObjectWebResponse<DeleteBucketResponse>)s3WebRequest.GetResponse();

        //     Assert.Equal(200, s3WebResponse.StatusCode);

        // }

        // 1. List no bucket
        [Fact, TestPriority(1)]
        public void S3ListBuckets()
        {
            System.Net.S3.S3WebRequest s3WebRequest = (System.Net.S3.S3WebRequest)WebRequest.Create("s3://bucket1");
            s3WebRequest.Method = "LSB";
            System.Net.S3.S3ObjectWebResponse<ListBucketsResponse> s3WebResponse = (System.Net.S3.S3ObjectWebResponse<ListBucketsResponse>)s3WebRequest.GetResponse();

            Assert.Equal(200, s3WebResponse.StatusCode);

            Assert.Equal(0, s3WebResponse.GetObject().Buckets.Count);
        }

        // 2. Create a bucket
        [Fact, TestPriority(2)]
        public async Task S3CreateBucket()
        {
            System.Net.S3.S3WebRequest s3WebRequest = (System.Net.S3.S3WebRequest)WebRequest.Create("s3://bucket1");
            s3WebRequest.Method = "MKB";
            Assert.Equal("bucket1", s3WebRequest.BucketName);
            System.Net.S3.S3ObjectWebResponse<PutBucketResponse> s3WebResponse =
                (System.Net.S3.S3ObjectWebResponse<PutBucketResponse>)(await s3WebRequest.GetResponseAsync());

            Assert.Equal(200, s3WebResponse.StatusCode);
        }

        // 3. List new bucket
        [Fact, TestPriority(3)]
        public void S3ListBuckets2()
        {
            System.Net.S3.S3WebRequest s3WebRequest = (System.Net.S3.S3WebRequest)WebRequest.Create("s3://localhost");
            s3WebRequest.Method = "LSB";
            System.Net.S3.S3ObjectWebResponse<ListBucketsResponse> s3WebResponse = (System.Net.S3.S3ObjectWebResponse<ListBucketsResponse>)s3WebRequest.GetResponse();

            Assert.Equal(200, s3WebResponse.StatusCode);

            Assert.Equal(1, s3WebResponse.GetObject().Buckets.Count);
            Assert.Equal("bucket1", s3WebResponse.GetObject().Buckets.First().BucketName);
        }

        // 4. Upload Test file
        [Fact, TestPriority(4)]
        public async Task S3UploadFile()
        {
            System.Net.S3.S3WebRequest s3WebRequest = (System.Net.S3.S3WebRequest)WebRequest.Create("s3://bucket1/testfile1.txt");
            s3WebRequest.Method = "POST";
            s3WebRequest.ContentLength = 128 * 1024 * 1024;
            s3WebRequest.ContentType = "application/octet-stream";

            Stream uploadStream = await s3WebRequest.GetRequestStreamAsync();
            Helpers.RunContentStreamGenerator(128 * 1024, uploadStream, 1024*1024);
            System.Net.S3.S3WebResponse s3WebResponse = (System.Net.S3.S3WebResponse)await s3WebRequest.GetResponseAsync();

            Assert.Equal(200, s3WebResponse.StatusCode);
        }

        // 5. List Test file
        [Fact, TestPriority(5)]
        public async Task S3ListUploadedFile()
        {
            System.Net.S3.S3WebRequest s3WebRequest = (System.Net.S3.S3WebRequest)WebRequest.Create("s3://bucket1/");
            s3WebRequest.Method = "LS";

            System.Net.S3.S3ObjectWebResponse<ListObjectsResponse> s3WebResponse =
                (System.Net.S3.S3ObjectWebResponse<ListObjectsResponse>)await s3WebRequest.GetResponseAsync();

            Assert.Equal(200, s3WebResponse.StatusCode);

            Assert.Equal(1, s3WebResponse.GetObject().S3Objects.Count);
            Assert.Equal(128 * 1024 * 1024, s3WebResponse.GetObject().S3Objects.First().Size);

        }

        // 6. List Test file
        [Fact, TestPriority(6)]
        public async Task S3CopyUploadedFile()
        {
            System.Net.S3.S3WebRequest s3WebRequest = (System.Net.S3.S3WebRequest)WebRequest.Create("s3://bucket1/testfile1.txt");
            s3WebRequest.Method = "CP";
            s3WebRequest.CopyTo = new Uri("s3://bucket1/testfile2.txt");

            System.Net.S3.S3ObjectWebResponse<CopyObjectResponse> s3WebResponse =
                (System.Net.S3.S3ObjectWebResponse<CopyObjectResponse>)await s3WebRequest.GetResponseAsync();

            Assert.Equal(200, s3WebResponse.StatusCode);

        }

        // 7. Download Test file
        [Fact, TestPriority(7)]
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

        // 8. Download Range Test file
        [Fact, TestPriority(8)]
        public async Task S3DownloadRangedFile()
        {
            System.Net.S3.S3WebRequest s3WebRequest = (System.Net.S3.S3WebRequest)WebRequest.Create("s3://bucket1/testfile2.txt");
            s3WebRequest.Method = "GETR";

            long i = 0;

            System.Net.S3.S3WebResponse s3WebResponse = (System.Net.S3.S3WebResponse)await s3WebRequest.GetResponseAsync();
            Assert.Equal(200, s3WebResponse.StatusCode);
            Assert.Equal("application/octet-stream", s3WebResponse.ContentType);
            Assert.Equal(128 * 1024 * 1024, s3WebResponse.ContentLength);

            using (Stream s3WebResponseStream = s3WebResponse.GetResponseStream())
            {
                s3WebResponseStream.Seek(120 * 1024 * 1024, 0);
                Assert.Equal(120 * 1024 * 1024, s3WebResponseStream.Position);
                int j = 0;
                do
                {
                    j = s3WebResponseStream.Read(new byte[1024], 0, 1024);
                    i += j;
                }
                while (j > 0);
            }

            Assert.Equal(8 * 1024 * 1024, i);

        }

        // 10. Delete Test file
        [Fact, TestPriority(10)]
        public async Task S3DeleteFile()
        {
            System.Net.S3.S3WebRequest s3WebRequest = (System.Net.S3.S3WebRequest)WebRequest.Create("s3://bucket1/testfile1.txt");
            s3WebRequest.Method = "RM";

            System.Net.S3.S3WebResponse s3WebResponse = (System.Net.S3.S3WebResponse)await s3WebRequest.GetResponseAsync();
            Assert.Equal(204, s3WebResponse.StatusCode);
        }
    }
}
