using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using Amazon.S3.Util;

namespace System.Net.S3
{
    public class S3UriParser : GenericUriParser
    {
        private static readonly Regex PathStyleRegEx = new Regex(@"^s3://(?'hostOrBucket'[^/]+)(/(?'key'.*))?$");

        GenericUriParser genericUriParser = new GenericUriParser(GetS3GenericUriParserOptions());

        public S3UriParser() : base(GetS3GenericUriParserOptions())
        {
        }

        public static AmazonS3Uri GetAmazonS3Uri(Uri uri)
        {
            AmazonS3Uri amazonS3Uri = null;
            var success = AmazonS3Uri.TryParseAmazonS3Uri(uri, out amazonS3Uri);
            return success ? amazonS3Uri : null;
        }

        protected override string Resolve(Uri baseUri, Uri relativeUri, out UriFormatException parsingError)
        {
            parsingError = null;
            if (string.IsNullOrEmpty(relativeUri.ToString()))
                return baseUri.ToString();
            string keypath = Path.GetDirectoryName(GetKey(baseUri));
            if (keypath == null) keypath = "";
            return string.Format("s3://{0}{1}",
                                GetBucketName(baseUri),
                                Path.GetFullPath("/" + Path.Combine(keypath, relativeUri.ToString()).TrimStart('/')));
        }

        protected override void InitializeAndValidate(Uri uri, out UriFormatException parsingError)
        {
            AmazonS3Uri amazonS3Uri = GetAmazonS3Uri(uri);
            if (amazonS3Uri != null)
            {
                parsingError = null;
                return;
            }

            Match match = PathStyleRegEx.Match(uri.OriginalString);
            if (!match.Success)
            {
                parsingError = new UriFormatException("S3 Uri could not be parsed");
                return;
            }
            parsingError = null;
        }

        public static string GetBucketName(Uri s3Uri)
        {
            AmazonS3Uri amazonS3Uri = GetAmazonS3Uri(s3Uri);
            if (amazonS3Uri != null)
                return amazonS3Uri.Bucket;

            Match match = PathStyleRegEx.Match(s3Uri.OriginalString);
            if (!match.Success)
            {
                throw new FormatException("Cannot parse S3 Uri");
            }
            return match.Groups["hostOrBucket"].Value;
        }

        public static string GetKey(Uri s3Uri)
        {
            AmazonS3Uri amazonS3Uri = GetAmazonS3Uri(s3Uri);
            if (amazonS3Uri != null)
                return amazonS3Uri.Key;

            Match match = PathStyleRegEx.Match(s3Uri.OriginalString);
            if (!match.Success)
            {
                throw new FormatException("Cannot parse S3 Uri");
            }
            return match.Groups["key"].Value;
        }

        private static GenericUriParserOptions GetS3GenericUriParserOptions()
        {
            return GenericUriParserOptions.Default
                | GenericUriParserOptions.NoQuery
                | GenericUriParserOptions.NoFragment
                | GenericUriParserOptions.NoPort
                | GenericUriParserOptions.AllowEmptyAuthority;
        }

        protected override string GetComponents(Uri uri, UriComponents uriComponents, UriFormat format)
        {
            Uri newUri = uri;
            Match match = PathStyleRegEx.Match(uri.OriginalString);
            if (match.Success)
            {
                if ((uriComponents == UriComponents.Path) ||
                                      (uriComponents == (UriComponents.Path | UriComponents.KeepDelimiter))) 
                                    return GetBucketName(uri) + "/" + GetKey(uri);

                if ((uriComponents == UriComponents.AbsoluteUri)) return uri.OriginalString;
                if (uriComponents == (UriComponents.SchemeAndServer | UriComponents.UserInfo) ||
                                   uriComponents == UriComponents.SchemeAndServer) return "s3://";
                if (uriComponents == UriComponents.Host) return "";
                if (uriComponents == UriComponents.StrongPort) return "";
                if (uriComponents == UriComponents.HttpRequestUrl) return uri.OriginalString;
            }

            return uri.OriginalString;
        }


    }
}