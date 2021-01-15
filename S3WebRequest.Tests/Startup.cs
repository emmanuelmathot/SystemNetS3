using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace S3WebRequest.Tests
{
    public class Startup
    {
        public IConfiguration Configuration { get; private set; }

        public void ConfigureServices(IServiceCollection services)
        {
            Configuration = GetApplicationConfiguration();
            services.AddLogging(builder =>
                {
                    builder.AddConfiguration(Configuration.GetSection("Logging"));
                });
            services.AddOptions();
            var awsOptions = Configuration.GetAWSOptions("AWS");
            services.AddDefaultAWSOptions(awsOptions);
        }

        public void Configure(ILoggerFactory loggerfactory, ITestOutputHelperAccessor accessor)
        {
            loggerfactory.AddProvider(new XunitTestOutputLoggerProvider(accessor));
        }

        public static IConfiguration GetApplicationConfiguration()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }
    }
}