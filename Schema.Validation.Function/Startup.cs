using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Schema.Validation.Functions;
using Schema.Validation.Functions.Services;
using Serilog;
using Serilog.Formatting.Compact;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Schema.Validation.Functions
{
    public class Startup : FunctionsStartup
    {
        private AppSettings _appSettings = new AppSettings();

        public override void Configure(IFunctionsHostBuilder builder)
        {
            var configuration = builder.Services
                .BuildServiceProvider()
                .GetRequiredService(typeof(IConfiguration)) as IConfiguration;

            _appSettings = new AppSettings();

            configuration.Bind(_appSettings);

            builder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddSerilog(new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .WriteTo.Console(new CompactJsonFormatter())
                    .CreateLogger());
            });

            builder.Services.AddSingleton<IAppSettings, AppSettings>();
            builder.Services.AddSingleton<ISchemaService, SchemaService>();
        }
    }
}
