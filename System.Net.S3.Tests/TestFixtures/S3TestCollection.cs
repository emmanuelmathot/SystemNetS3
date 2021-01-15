using Xunit;
using System.Net;
using System.Net.Http;

namespace System.Net.S3.Tests
{
    [CollectionDefinition(nameof(S3TestCollection))]
    public class S3TestCollection : ICollectionFixture<LocalStackFixture>, ICollectionFixture<Logging>, ICollectionFixture<WebRequestFixture>
    {
    }
}