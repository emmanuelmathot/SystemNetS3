using System.Threading.Tasks;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Modules;
using DotNet.Testcontainers.Containers.OutputConsumers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace S3WebRequest.Tests
{
    public class LocalStackFixture : IAsyncLifetime
    {
        private readonly TestcontainersContainer _localStackContainer;

        public ServiceCollection Collection { get; }

        public LocalStackFixture()
        {
            var localStackBuilder = new TestcontainersBuilder<TestcontainersContainer>()
                .WithImage("localstack/localstack")
                .WithCleanUp(true)
                .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole())
                .WithEnvironment("DEFAULT_REGION", "eu-central-1")
                .WithEnvironment("SERVICES", "s3")
                .WithEnvironment("DOCKER_HOST", "unix:///var/run/docker.sock")
                .WithEnvironment("DEBUG", "1")
                .WithPortBinding(4566, 4566);

            _localStackContainer = localStackBuilder.Build();
        }
        public async Task InitializeAsync()
        {
            await _localStackContainer.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await _localStackContainer.StopAsync();
        }
    }
}