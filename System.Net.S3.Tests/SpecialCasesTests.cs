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
    public class SpecialCasesTests : BaseTests
    {
        // 0. List no bucket
        [Fact, TestPriority(0)]
        public void S3ListBuckets()
        {
            System.Net.S3.S3WebRequest s3WebRequest = (System.Net.S3.S3WebRequest)WebRequest.Create("s3://bucket");
            s3WebRequest.Method = "LSB";
            System.Net.S3.S3ObjectWebResponse<ListBucketsResponse> s3WebResponse = (System.Net.S3.S3ObjectWebResponse<ListBucketsResponse>)s3WebRequest.GetResponse();

            Assert.Equal(200, s3WebResponse.StatusCode);

            Assert.Equal(0, s3WebResponse.GetObject().Buckets.Count);
        }

        // 1. Create a bucket
        [Fact, TestPriority(1)]
        public async Task S3CreateBucket()
        {
            System.Net.S3.S3WebRequest s3WebRequest = (System.Net.S3.S3WebRequest)WebRequest.Create("s3://bucket");
            s3WebRequest.Method = "MKB";
            Assert.Equal("bucket", s3WebRequest.BucketName);
            System.Net.S3.S3ObjectWebResponse<PutBucketResponse> s3WebResponse =
                (System.Net.S3.S3ObjectWebResponse<PutBucketResponse>)(await s3WebRequest.GetResponseAsync());

            Assert.Equal(200, s3WebResponse.StatusCode);
        }

        // 1. Create a bucket
        [Fact, TestPriority(1)]
        public async Task S3DeleteBucket()
        {
            System.Net.S3.S3WebRequest s3WebRequest = (System.Net.S3.S3WebRequest)WebRequest.Create("s3://bucket");
            s3WebRequest.Method = "RMB";
            Assert.Equal("bucket", s3WebRequest.BucketName);
            System.Net.S3.S3ObjectWebResponse<DeleteBucketResponse> s3WebResponse =
                (System.Net.S3.S3ObjectWebResponse<DeleteBucketResponse>)(await s3WebRequest.GetResponseAsync());

            Assert.Equal(204, s3WebResponse.StatusCode);
        }
   
    }
}
