using System;
using System.Collections.Generic;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using BH.DIS.Alerts;
using BH.DIS.MessageStore.States;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BH.DIS.Alerts
{
    public class Function
    {
        private readonly IAlertService _alertSvc;
        public Function(AlertService alert)
        {
            _alertSvc = alert;
        }

        [FunctionName("AlertFunction")]
        public void Run([QueueTrigger("eg-notify-queue", Connection = "AzureWebJobsStorage")] string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");            
            QueueEvent gridEvent = JsonConvert.DeserializeObject<QueueEvent>(myQueueItem);
            log.LogInformation($"Deserialized: {gridEvent}");
            _alertSvc.RunAlertService(log, gridEvent);            
        }
    }
}
