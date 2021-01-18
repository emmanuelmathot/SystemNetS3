using System;
using System.Net.S3;
using System.Reflection;
using Amazon;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Util;
using Microsoft.Extensions.Logging;

namespace System.Net.S3
{
    public class S3WebRequestCreate : IWebRequestCreate
    {
        private readonly ILogger logger;
        private Amazon.Extensions.NETCore.Setup.AWSOptions options;
        private readonly S3BucketsOptions s3BucketsConfiguration;

        public S3WebRequestCreate(ILogger logger, Amazon.Extensions.NETCore.Setup.AWSOptions options, S3BucketsOptions s3BucketsConfiguration = null)
        {
            this.logger = logger;
            this.options = options;
            this.s3BucketsConfiguration = s3BucketsConfiguration;
        }

        public WebRequest Create(Uri uri)
        {
            IAmazonS3 client = null;
            // Create client from config
            try
            {
                if (options != null)
                    client = options?.CreateServiceClient<IAmazonS3>();
            }
            catch(TargetInvocationException e)
            {
                throw e.InnerException;
            }
            S3WebRequest s3WebRequest = new S3WebRequest(uri, logger, (AmazonS3Client)client, s3BucketsConfiguration);
            return s3WebRequest;
        }
    }
}
