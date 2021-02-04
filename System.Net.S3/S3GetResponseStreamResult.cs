using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;

namespace System.Net.S3
{
    internal class S3GetResponseStreamResult : IAsyncResult
    {
        private readonly Task<AmazonWebServiceResponse> task;
        private object state;

        public S3GetResponseStreamResult(Task<AmazonWebServiceResponse> task, object state)
        {
            this.task = task;
            this.state = state;
        }

        public object AsyncState => state;

        public WaitHandle AsyncWaitHandle => ((IAsyncResult)task).AsyncWaitHandle;

        public bool CompletedSynchronously => task.IsCompleted;

        public bool IsCompleted => task.IsCompleted;
    }
}