using BH.DIS.Core;
using BH.DIS.Core.Logging;
using BH.DIS.MessageStore;
using BH.DIS.SDK.Logging;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;

[assembly: FunctionsStartup(typeof(BH.DIS.Alerts.Startup))]

namespace BH.DIS.Alerts
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            BuildServices(builder.Services);
        }

        private void BuildServices(IServiceCollection services)
        {
            services.AddSingleton<ILoggerProvider>(sp => CreateLoggerProvider(sp));

            services.AddSingleton<ICosmosDbClient>(sp => CreateCosmosClient(sp));

            services.AddScoped<PlatformConfiguration>();

            services.AddScoped<AlertService>();
        }

        private ICosmosDbClient CreateCosmosClient(IServiceProvider sp)
        {
            var config = sp.GetRequiredService<IConfiguration>();
            string globalTraceLogInstrKey = config.GetValue<string>("GlobalTraceLogInstrKey");
            string connectionString = config.GetValue<string>("CosmosConnection");

            Serilog.ILogger baseLogger = CreateSerilogLogger(globalTraceLogInstrKey);

            var cosmosClient = new CosmosClient(connectionString);
            return new CosmosDbClient(cosmosClient, new SerilogAdapter(baseLogger));
        }

        private ILoggerProvider CreateLoggerProvider(IServiceProvider sp)
        {
            var config = sp.GetRequiredService<IConfiguration>();
            string globalTraceLogInstrKey = config.GetValue<string>("GlobalTraceLogInstrKey");

            Serilog.ILogger baseLogger = CreateSerilogLogger(globalTraceLogInstrKey);

            return new LoggerProvider(baseLogger);
        }

        private static Serilog.ILogger CreateSerilogLogger(string globalTraceLogInstrKey)
        {
            return new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .WriteTo.ApplicationInsights(globalTraceLogInstrKey, TelemetryConverter.Traces)
                .CreateLogger();
        }
    }
}
