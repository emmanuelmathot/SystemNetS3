namespace System.Net.S3
{
    public static class S3RequestMethods
    {
        public const string DownloadObject = "GET";
        public const string DownloadRangedObject = "GETR";
        public const string CreateBucket = "MKB";
        public const string ListObject = "LS";
        public const string RemoveBucket = "RMB";
        public const string DeleteObject = "RM";
        public const string Move = "MV";
        public static string UploadObject = "POST";
        public static string ListBuckets = "LSB";
        public static string Copy = "CP";
    }
}