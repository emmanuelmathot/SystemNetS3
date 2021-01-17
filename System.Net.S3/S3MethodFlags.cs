namespace System.Net.S3
{
    internal enum S3MethodFlags
    {
        IsDownload,
        IsUpload,
        HasHttpCommand,
        ShouldParseForResponseUri,
        TakesParameter,
        MayTakeParameter,
        ParameterIsBucket,
        ParameterIsKey
    }
}