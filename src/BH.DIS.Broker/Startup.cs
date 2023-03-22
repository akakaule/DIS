using Azure.Messaging.ServiceBus;
using BH.DIS.Broker.Services;
using BH.DIS.Core.Logging;
using BH.DIS.Core.Messages;
using BH.DIS.SDK.Logging;
using BH.DIS.ServiceBus;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;

[assembly: FunctionsStartup(typeof(BH.DIS.Broker.Startup))]

namespace BH.DIS.Broker
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
            
            var serviceBusConnection = config.GetValue<string>("AzureWebJobsServiceBus");
            var endpoint = config.GetValue<string>("BrokerId");

            ServiceBusClient serviceBusClient = new ServiceBusClient(serviceBusConnection);
            var serviceBusSender = serviceBusClient.CreateSender(endpoint);

            services.AddSingleton<ISender>(sp => new Sender(serviceBusSender));

            services.AddSingleton<IEventContextHandler, BrokerService>();

            services.AddSingleton<ILoggerProvider>(sp => CreateLoggerProvider(sp));

            services.AddSingleton<IMessageHandler, BrokerMessageHandler>();

            services.AddSingleton<IResponseService, ResponseService>();

            services.AddSingleton<IServiceBusAdapter, ServiceBusAdapter>();

        }

        private ILoggerProvider CreateLoggerProvider(IServiceProvider sp)
        {
            var config = sp.GetRequiredService<IConfiguration>();
            string globalTraceLogInstrKey = config.GetValue<string>("GlobalTraceLogInstrKey");

            Serilog.ILogger baseLogger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.ApplicationInsights(globalTraceLogInstrKey, TelemetryConverter.Traces)
                .CreateLogger();

            return new LoggerProvider(baseLogger);
        }
    }
}