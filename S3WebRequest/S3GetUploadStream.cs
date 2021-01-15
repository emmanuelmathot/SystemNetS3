using System.Threading;

namespace System.Net.S3
{
    internal class S3GetUploadStream : IAsyncResult
    {
        private BlockingStream stream;
        private object state;
        private AsyncCallback callback;
        private ManualResetEvent m_streamReady;

        public S3GetUploadStream(BlockingStream stream, object state, AsyncCallback callback)
        {
            this.stream = stream;
            this.state = state;
            this.callback = callback;
            m_streamReady = new ManualResetEvent(true);
        }

        public object AsyncState => state;

        public WaitHandle AsyncWaitHandle => m_streamReady;

        public bool CompletedSynchronously => true;

        public bool IsCompleted => true;
    }
}