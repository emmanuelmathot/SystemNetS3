using System;
using System.Net;
using Xunit;

namespace System.Net.S3.Tests
{
    public class BaseTests
    {

        [Fact]
        public void S3WebRequestCreateTest()
        {
            var s3WebRequest = WebRequest.Create("s3://localhost");
            Assert.IsType(typeof(System.Net.S3.S3WebRequest), s3WebRequest);
        }

    }
}
