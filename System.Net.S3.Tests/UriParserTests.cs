using System;
using System.Net;
using Xunit;

namespace System.Net.S3.Tests
{
    [Collection(nameof(S3TestCollection))]
    public class S3UriParserTests
    {

        [Fact]
        public void UriParserIsKnowScheme()
        {
            Assert.True(UriParser.IsKnownScheme("s3"));
        }

        [Fact]
        public void UriParserGenericS3()
        {
            Uri s3 = new Uri("s3://processing_results");
            Assert.Equal("", s3.Host);
            Assert.Equal("", s3.LocalPath);
            Assert.Equal("processing_results/", s3.AbsolutePath);
            Assert.Equal(-1, s3.Port);
            Assert.Equal("", s3.Authority);
            Assert.Equal("", s3.DnsSafeHost);
            Assert.Equal(true, s3.IsAbsoluteUri);
            Assert.Equal("processing_results", S3UriParser.GetBucketName(s3));
            Assert.Equal("", S3UriParser.GetKey(s3));
            var s3f = new Uri(s3, "file.jpg");
            Assert.Equal("processing_results/file.jpg", s3f.AbsolutePath);
        }

        [Fact]
        public void UriParserGenericS3_2()
        {
            Uri s3 = new Uri("s3://processing_results/test");
            Assert.Equal("", s3.Host);
            Assert.Equal("", s3.LocalPath);
            Assert.Equal("processing_results/test", s3.AbsolutePath);
            Assert.Equal(-1, s3.Port);
            Assert.Equal("", s3.Authority);
            Assert.Equal("", s3.DnsSafeHost);
            Assert.Equal(true, s3.IsAbsoluteUri);
            Assert.Equal("processing_results", S3UriParser.GetBucketName(s3));
            Assert.Equal("test", S3UriParser.GetKey(s3));
            var s3f = new Uri(s3, "file.jpg");
            Assert.Equal("processing_results/test/file.jpg", s3f.AbsolutePath);
        }

        [Fact]
        public void UriParserGenericS3_3()
        {
            Uri s3 = new Uri("s3://processing_results/test/file.png");
            Assert.Equal("", s3.Host);
            Assert.Equal("", s3.LocalPath);
            Assert.Equal("processing_results/test/file.png", s3.AbsolutePath);
            Assert.Equal("processing_results", S3UriParser.GetBucketName(s3));
            Assert.Equal("test/file.png", S3UriParser.GetKey(s3));
            var s3f = new Uri(s3, "file.jpg");
            Assert.Equal("processing_results/test/file.png/file.jpg", s3f.AbsolutePath);
            s3f = new Uri(s3, "/file.jpg");
            Assert.Equal("processing_results/file.jpg", s3f.AbsolutePath);
        }

        [Fact]
        public void UriParserOpenStackS3()
        {
            Uri s3 = new Uri("s3://user1:processing_results/test");
            Assert.Equal("", s3.Host);
            Assert.Equal("", s3.LocalPath);
            Assert.Equal("user1:processing_results/test", s3.AbsolutePath);
            Assert.Equal("user1:processing_results", S3UriParser.GetBucketName(s3));
            Assert.Equal("test", S3UriParser.GetKey(s3));
            var s3f = new Uri(s3, "file.jpg");
            Assert.Equal("user1:processing_results/test/file.jpg", s3f.AbsolutePath);
        }

        [Fact]
        public void UriParserGenericS3WithSlash()
        {
            Uri s3 = new Uri("s3://processing_results/");
            Assert.Equal("", s3.Host);
            Assert.Equal("processing_results", S3UriParser.GetBucketName(s3));
            Assert.Equal("", S3UriParser.GetKey(s3));
        }

        [Fact]
        public void UriParserAwsPathStyleUri()
        {
            Uri s3 = new Uri("https://s3.us-west-2.amazonaws.com/mybucket/puppy.jpg");
            Assert.Equal("s3.us-west-2.amazonaws.com", s3.Host);
            Assert.Equal("mybucket", S3UriParser.GetBucketName(s3));
            Assert.Equal("puppy.jpg", S3UriParser.GetKey(s3));
        }

        [Fact]
        public void UriParserAwsVirtualHostUri()
        {
            Uri s3 = new Uri("https://my-bucket.s3.us-west-2.amazonaws.com/puppy.png");
            Assert.Equal("my-bucket.s3.us-west-2.amazonaws.com", s3.Host);
            Assert.Equal("my-bucket", S3UriParser.GetBucketName(s3));
            Assert.Equal("puppy.png", S3UriParser.GetKey(s3));
        }

        

    }
}
