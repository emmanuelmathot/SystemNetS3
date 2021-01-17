using System.Text.RegularExpressions;
using Amazon.S3.Util;

namespace System.Net.S3
{
    public class S3UriParser : GenericUriParser
    {
        private readonly Regex regEx = new Regex(@"^s3://(?'hostOrBucket'[^/]*)(/.*)?$");

        public S3UriParser() : base(GetS3GenericUriParserOptions())
        {
        }

        private static GenericUriParserOptions GetS3GenericUriParserOptions()
        {
            return GenericUriParserOptions.Default
                | GenericUriParserOptions.AllowEmptyAuthority
                | GenericUriParserOptions.NoFragment
                | GenericUriParserOptions.NoPort
                | GenericUriParserOptions.NoQuery
                | GenericUriParserOptions.NoUserInfo;
        }

        protected override string GetComponents(Uri uri, UriComponents uriComponents, UriFormat format)
        {
            Uri newUri = uri;
            Match match = regEx.Match(uri.OriginalString);
            if (match.Success && (uriComponents == UriComponents.Host || uriComponents == UriComponents.Path))
            {
                if (uriComponents == UriComponents.Host)
                {
                    try
                    {
                        Dns.GetHostEntry(match.Groups["hostOrBucket"].Value);
                        return base.GetComponents(newUri, uriComponents, format);
                    }
                    catch
                    {
                        return "";
                    }
                }
                if (uriComponents == UriComponents.Path)
                {
                    try
                    {
                        Dns.GetHostEntry(match.Groups["hostOrBucket"].Value);
                        return base.GetComponents(newUri, uriComponents, format);
                    }
                    catch
                    {
                        return uri.OriginalString.Replace("s3:/", "");
                    }
                }
                UriBuilder uriBuilder = new UriBuilder(uri);
                uriBuilder.Path = "/" + uriBuilder.Host + uriBuilder.Path;
                uriBuilder.Host = null;
                newUri = uriBuilder.Uri;
            }

            return base.GetComponents(newUri, uriComponents, format);
        }
    }
}