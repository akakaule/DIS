using BH.DIS.Broker.Services;
using BH.DIS.Core.Logging;
using BH.DIS.MessageStore;
using BH.DIS.SDK.Logging;
using BH.DIS.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;

[assembly: FunctionsStartup(typeof(BH.DIS.Resolver.Startup))]

namespace BH.DIS.Resolver
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            BuildServices(builder.Services);
        }

        private void BuildServices(IServiceCollection services)
        {
            var sp = services.BuildServiceProvider();
            var config = sp.GetRequiredService<IConfiguration>();
            string globalTraceLogInstrKey = config.GetValue<string>("GlobalTraceLogInstrKey");
            Log.Logger = CreateSerilogLogger(globalTraceLogInstrKey);

            services.AddSingleton<IServiceBusAdapter>(sp => CreateServiceBusAdapter(sp));
        }

        private IServiceBusAdapter CreateServiceBusAdapter(IServiceProvider sp)
        {
            var config = sp.GetRequiredService<IConfiguration>();
            string cosmosConnectionString = config.GetValue<string>("CosmosConnection");
            string messageStorageConnectionString = config.GetValue<string>("MessageStoreStorageConnection");

            var cosmosClient = new CosmosClient(cosmosConnectionString);
            var cosmosDbClient = new CosmosDbClient(cosmosClient, new SerilogAdapter(Log.Logger));

            var messageStoreClient = new MessageStoreClient(messageStorageConnectionString);

            ILoggerProvider loggerProvider = new LoggerProvider(Log.Logger);

            var resolverService = new ResolverService(loggerProvider, messageStoreClient, cosmosDbClient);

            return new ServiceBusAdapter(resolverService);
        }

        private static Serilog.ILogger CreateSerilogLogger(string globalTraceLogInstrKey)
        {
            return new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.ApplicationInsights(globalTraceLogInstrKey, TelemetryConverter.Traces)
                .CreateLogger();
        }
    }
}