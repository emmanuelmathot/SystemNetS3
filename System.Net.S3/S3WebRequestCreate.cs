using System;
using System.Net.S3;
using System.Reflection;
using Amazon;
using Amazon.Extensions.NETCore.Setup;
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
            AmazonS3Uri amazonS3Uri = null;
            // Create client from config
            AmazonS3Uri.TryParseAmazonS3Uri(uri, out amazonS3Uri);
            AWSOptions awsOptions = options;
            if (options == null)
            {
                awsOptions = new AWSOptions();
            }
            client = awsOptions.CreateServiceClient<IAmazonS3>();
            if ( amazonS3Uri == null || amazonS3Uri.IsPathStyle || S3UriParser.IsKnownScheme(uri.Scheme) ){
                (client.Config as AmazonS3Config).ForcePathStyle = true;
            }
            

            S3WebRequest s3WebRequest = new S3WebRequest(uri, logger, (AmazonS3Client)client, s3BucketsConfiguration);
            return s3WebRequest;
        }
    }
}
