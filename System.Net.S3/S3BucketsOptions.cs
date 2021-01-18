using System.Collections;
using System.Collections.Generic;

namespace System.Net.S3
{
    public class S3BucketsOptions : Dictionary<string, S3BucketOptions>
    {
    }

    public class S3BucketOptions
    {
        public string Payer { get; set; }
    }
}