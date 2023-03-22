using Azure.Messaging.ServiceBus;
using BH.DIS.Core.Logging;
using BH.DIS.MessageStore;
using BH.DIS.SDK;
using BH.DIS.SDK.Logging;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;

[assembly: FunctionsStartup(typeof(BH.DIS.Heartbeat.Startup))]

namespace BH.DIS.Heartbeat;
public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        BuildServices(builder.Services);
    }

    private void BuildServices(IServiceCollection services)
    {
        services.AddSingleton<ILoggerProvider>(sp => CreateLoggerProvider(sp));
        services.AddSingleton<IPublisherClient>(sp =>
        {
            var config = sp.GetService<IConfiguration>();
            var serviceBusConnection = config["AzureWebJobsServiceBus"];
            var serviceBusClient = new ServiceBusClient(serviceBusConnection);
            var loggerProvider = new LoggerProvider(Log.Logger);

            return new PublisherClient(serviceBusClient, "Heartbeat", loggerProvider);
        });

        services.AddSingleton<ISubscriberClient>(sp =>
        {
            var config = sp.GetService<IConfiguration>();
            var serviceBusConnection = config["AzureWebJobsServiceBus"];
            var serviceBusClient = new ServiceBusClient(serviceBusConnection);
            var loggerProvider = new LoggerProvider(Log.Logger);

            return new SubscriberClient(serviceBusClient, "Heartbeat", loggerProvider);
        });

        services.AddSingleton<ICosmosDbClient>(sp =>
        {
            var config = sp.GetService<IConfiguration>();
            var cosmosConnection = config["CosmosConnectionString"];
            var cosmosClient = new CosmosClient(cosmosConnection);
            return new CosmosDbClient(cosmosClient);
        });
    }

    private BH.DIS.Core.Logging.ILoggerProvider CreateLoggerProvider(IServiceProvider sp)
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
