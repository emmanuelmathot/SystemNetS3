using System.Text.RegularExpressions;
using Amazon.S3.Util;

namespace System.Net.S3
{
    public class S3UriParser : UriParser
    {
        private readonly Regex regEx = new Regex(@"^s3://(?'hostOrBucket'[^/]*)(/.*)?$");

        GenericUriParser genericUriParser = new GenericUriParser(GetS3GenericUriParserOptions());

        public S3UriParser()
        {
        }
        protected override string Resolve(Uri baseUri, Uri relativeUri, out UriFormatException parsingError)
        {
            parsingError = null;
            return baseUri.ToString() + relativeUri.ToString();
        }

        protected override void InitializeAndValidate(Uri uri, out UriFormatException parsingError)
        {
            Match match = regEx.Match(uri.OriginalString);
            if (!match.Success)
            {
                parsingError = new UriFormatException("S3 Uri could not be parsed");
            }
            else
            {
                parsingError = null;
            }
        }

        private static GenericUriParserOptions GetS3GenericUriParserOptions()
        {
            return GenericUriParserOptions.Default
                | GenericUriParserOptions.NoQuery
                | GenericUriParserOptions.NoFragment
                | GenericUriParserOptions.NoPort;
        }

        protected override string GetComponents(Uri uri, UriComponents uriComponents, UriFormat format)
        {
            Uri newUri = uri;
            Match match = regEx.Match(uri.OriginalString);
            if (match.Success && (uriComponents == UriComponents.Path) ||
                                  (uriComponents == (UriComponents.Path | UriComponents.KeepDelimiter)))
            {
                return uri.ToString().Replace("s3://","").Replace(match.Groups["hostOrBucket"].Value, "");
            }
            if (match.Success && (uriComponents == UriComponents.AbsoluteUri))
            {
                return uri.OriginalString;
            }
            if (match.Success && (uriComponents == (UriComponents.SchemeAndServer | UriComponents.UserInfo) ||
                                   uriComponents == UriComponents.SchemeAndServer))
            {
                return "s3://" + match.Groups["hostOrBucket"].Value;
            }
            if (match.Success && (uriComponents == UriComponents.Host))
            {
                return match.Groups["hostOrBucket"].Value;
            }

            return "";
        }
    }
}