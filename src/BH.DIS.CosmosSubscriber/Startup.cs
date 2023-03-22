using BH.DIS.Core.Logging;
using BH.DIS.SDK.Logging;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using System;


[assembly: FunctionsStartup(typeof(BH.DIS.CosmosSubscriber.Startup))]


namespace BH.DIS.CosmosSubscriber
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
            //services.AddSingleton<CosmosFunction, CosmosFunction>();
        }

        private ILoggerProvider CreateLoggerProvider(IServiceProvider sp)
        {
            var config = sp.GetRequiredService<IConfiguration>();
            string globalTraceLogInstrKey = config.GetValue<string>("GlobalTraceLogInstrKey");

            Serilog.ILogger baseLogger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.ApplicationInsights(globalTraceLogInstrKey, TelemetryConverter.Traces)
                .CreateLogger();

            return new LoggerProvider(baseLogger);
        }
    }
}
