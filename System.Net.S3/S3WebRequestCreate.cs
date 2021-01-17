using System;
using System.Net.S3;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Util;
using Microsoft.Extensions.Logging;

namespace System.Net.S3
{
    public class S3WebRequestCreate : IWebRequestCreate
    {
        private readonly ILogger logger;
        private readonly Amazon.Extensions.NETCore.Setup.AWSOptions options;

        public S3WebRequestCreate(ILogger logger, Amazon.Extensions.NETCore.Setup.AWSOptions options)
        {
            this.logger = logger;
            this.options = options;
        }

        public WebRequest Create(Uri uri)
        {
            IAmazonS3 client = null;
            // Create client from config
            if ( options != null )
                client = options?.CreateServiceClient<IAmazonS3>();
            // if no config, then let's try to create client only from uri
            if (client == null)
                client = CreateClientFromUri(uri);
            S3WebRequest s3WebRequest = new S3WebRequest(uri, logger, (AmazonS3Client)client);
            return s3WebRequest;
        }

        public static AmazonS3Client CreateClientFromUri(Uri uri)
        {
            AmazonS3Config config = new AmazonS3Config();
            try
            {
                AmazonS3Uri amazonS3Uri = new AmazonS3Uri(uri);
                config.RegionEndpoint = amazonS3Uri.Region;
            }
            catch
            {
                config.ServiceURL = "http://" + uri.Host;
            }
            return new AmazonS3Client(config);
        }
    }
}
