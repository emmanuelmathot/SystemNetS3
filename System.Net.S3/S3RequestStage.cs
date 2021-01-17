namespace System.Net.S3
{
    internal enum S3RequestStage
    {
        CheckForError = 0,  // Do nothing except if there is an error then auto promote to ReleaseConnection
        RequestStarted,     // Mark this request as started
        WriteReady,         // First half is done, i.e. either writer or response stream. This is always assumed unless Started or CheckForError
        ReadReady,          // Second half is done, i.e. the read stream can be accesses.
        ReleaseConnection   // Release the control connection (request is read i.e. done-done)
    }
}