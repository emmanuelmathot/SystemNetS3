using System.Globalization;

namespace System.Net.S3
{
    internal class S3MethodInfo
    {
        internal string Method;
        internal S3Operation Operation;
        internal S3MethodFlags Flags;
        internal string HttpCommand;

        internal S3MethodInfo(string method,
                               S3Operation operation,
                               S3MethodFlags flags,
                               string httpCommand)
        {
            Method = method;
            Operation = operation;
            Flags = flags;
            HttpCommand = httpCommand;
        }

        internal bool HasFlag(S3MethodFlags flags)
        {
            return (Flags & flags) != 0;
        }

        internal bool IsCommandOnly
        {
            get { return (Flags & (S3MethodFlags.IsDownload | S3MethodFlags.IsUpload)) == 0; }
        }

        internal bool IsUpload
        {
            get { return (Flags & S3MethodFlags.IsUpload) != 0; }
        }

        internal bool IsDownload
        {
            get { return (Flags & S3MethodFlags.IsDownload) != 0; }
        }

        internal bool HasHttpCommand
        {
            get { return (Flags & S3MethodFlags.HasHttpCommand) != 0; }
        }

        /// <summary>
        ///    <para>True if we should attempt to get a response uri
        ///    out of a server response</para>
        /// </summary>
        internal bool ShouldParseForResponseUri
        {
            get { return (Flags & S3MethodFlags.ShouldParseForResponseUri) != 0; }
        }

        internal static S3MethodInfo GetMethodInfo(string method)
        {
            method = method.ToUpper(CultureInfo.InvariantCulture);
            foreach (S3MethodInfo methodInfo in KnownMethodInfo)
                if (method == methodInfo.Method)
                    return methodInfo;
            // We don't support generic methods
            throw new ArgumentException(string.Format("invalid method {0} for S3", method));
        }

        static readonly S3MethodInfo[] KnownMethodInfo =
        {
            new S3MethodInfo(S3RequestMethods.DownloadObject,
                              S3Operation.GetObject,
                              S3MethodFlags.IsDownload
                              | S3MethodFlags.HasHttpCommand
                              | S3MethodFlags.TakesParameter
                              | S3MethodFlags.ParameterIsBucket
                              | S3MethodFlags.ParameterIsKey,
                              "GET"),
            new S3MethodInfo(S3RequestMethods.DownloadRangedObject,
                              S3Operation.GetSeekableObject,
                              S3MethodFlags.IsDownload
                              | S3MethodFlags.HasHttpCommand
                              | S3MethodFlags.TakesParameter
                              | S3MethodFlags.ParameterIsBucket
                              | S3MethodFlags.ParameterIsKey,
                              "GET"),
            new S3MethodInfo(S3RequestMethods.ListObject,
                              S3Operation.ListObject,
                              S3MethodFlags.HasHttpCommand
                              | S3MethodFlags.TakesParameter
                              | S3MethodFlags.ParameterIsBucket
                              | S3MethodFlags.ParameterIsKey,
                              "HEAD"),
            new S3MethodInfo(S3RequestMethods.ListBuckets,
                              S3Operation.ListBuckets,
                              S3MethodFlags.HasHttpCommand
                              | S3MethodFlags.MayTakeParameter,
                              "GET"),
            new S3MethodInfo(S3RequestMethods.UploadObject,
                              S3Operation.PutObject,
                              S3MethodFlags.IsUpload
                              | S3MethodFlags.TakesParameter,
                              "POST"),
            new S3MethodInfo(S3RequestMethods.DeleteObject,
                              S3Operation.DeleteObject,
                              S3MethodFlags.TakesParameter,
                              "DELETE"),
            new S3MethodInfo(S3RequestMethods.Move,
                              S3Operation.MoveObjects,
                              S3MethodFlags.TakesParameter,
                              "MV"),
            new S3MethodInfo(S3RequestMethods.Copy,
                              S3Operation.CopyObject,
                              S3MethodFlags.TakesParameter,
                              "MV"),
            new S3MethodInfo(S3RequestMethods.CreateBucket,
                              S3Operation.PutBucket,
                              S3MethodFlags.TakesParameter
                              | S3MethodFlags.ParameterIsBucket,
                              null),
            new S3MethodInfo(S3RequestMethods.RemoveBucket,
                              S3Operation.RemoveBucket,
                              S3MethodFlags.TakesParameter
                              | S3MethodFlags.ParameterIsBucket,
                              null),
        };

    }
}