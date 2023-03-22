using Azure.Messaging.ServiceBus;
using BH.DIS.MessageStore;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;

namespace BH.DIS.CommandLine
{
    static class CommandRunner
    {
        public static async Task Run(CommandOption sbConnectionString, CommandOption dbConnectionString, Func<ServiceBusClient, CosmosDbClient, ServiceBusAdministrationClient, Task> func)
        {
            var serviceBusConnectionStringToUse = sbConnectionString.HasValue() ? sbConnectionString.Value() : Environment.GetEnvironmentVariable(SbConnectionStringEnvName);
            var cosmosConnectionStringToUse = dbConnectionString.HasValue() ? dbConnectionString.Value() : Environment.GetEnvironmentVariable(DbConnectionStringEnvName);

            var serviceBusClient = new ServiceBusClient(serviceBusConnectionStringToUse);
            var serviceBusAdmin = new ServiceBusAdministrationClient(serviceBusConnectionStringToUse);
 
            var cosmosClient = new CosmosClient(cosmosConnectionStringToUse);
            var cosmosDbClient = new CosmosDbClient(cosmosClient);


            await func(serviceBusClient, cosmosDbClient, serviceBusAdmin);
        }

        public const string SbConnectionStringEnvName = "AzureServiceBus_ConnectionString";
        public const string DbConnectionStringEnvName = "CosmosDb_ConnectionString";
    }
}
