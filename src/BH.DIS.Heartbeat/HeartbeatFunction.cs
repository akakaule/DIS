using Azure.Messaging.ServiceBus;
using BH.DIS.MessageStore;
using BH.DIS.MessageStore.States;
using BH.DIS.SDK;
using BH.DIS.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using ServiceBusMessage = BH.DIS.ServiceBus.ServiceBusMessage;

namespace BH.DIS.Heartbeat;

public class HeartbeatFunction
{
    private readonly IPublisherClient _publisherClient;
    private readonly ICosmosDbClient _cosmosDbClient;
    private readonly HttpClient _httpClient;

    public HeartbeatFunction(IPublisherClient publisherClient, ICosmosDbClient cosmosDbClient, HttpClient httpClient)
    {
        this._publisherClient = publisherClient;
        this._cosmosDbClient = cosmosDbClient;
        this._httpClient = httpClient;
    }

    [FunctionName("SendHeartbeats")]
    public async Task SendHeartbeats([TimerTrigger("0 */5 * * * *", RunOnStartup = true)] TimerInfo myTimer, Microsoft.Extensions.Logging.ILogger log)
    {
        var @event = new Core.Events.Heartbeat
        {
            ForwardSendTime = DateTime.Now,
        };
        var messageId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();

        // Get endpoint metadata for all endpoints who have heartbeat enabled
        var enabledEndpoints = await _cosmosDbClient.GetMetadatasWithEnabledHeartbeat();

        foreach (var endpoint in enabledEndpoints)
        {
            var heartbeatDto = new MessageStore.States.Heartbeat
            {
                MessageId = messageId,
                StartTime = @event.ForwardSendTime,
                EndpointHeartbeatStatus = HeartbeatStatus.Pending
            };
            await _cosmosDbClient.SetHeartbeat(heartbeatDto, endpoint.EndpointId);
            await PublishToStoragehook(endpoint.EndpointId);
        }

        log.LogInformation("Publish Heartbeats");
        await _publisherClient.Publish(@event, "HeartBeat", correlationId, messageId);
    }

    [FunctionName("ReceiveHeartbeats")]
    public async Task ReceiveHeartbeats(
        [ServiceBusTrigger(topicName: "Heartbeat", subscriptionName: "Heartbeat", Connection = "AzureWebJobsServiceBus",
            IsSessionsEnabled = true)]
        ServiceBusReceivedMessage message,
        ServiceBusSessionMessageActions sessionActions,
        ServiceBusReceiveActions receiveActions)
    {
        var messageWrapper = new ServiceBusMessage(message);
        var sessionWrapper = new ServiceBusSession(sessionActions, receiveActions);
        var messageContext = new MessageContext(messageWrapper, sessionWrapper);

        var heartbeatEvent =
            JsonConvert.DeserializeObject<Core.Events.Heartbeat>(messageContext.MessageContent.EventContent.EventJson);

        var heartbeatDto = new MessageStore.States.Heartbeat
        {
            MessageId = messageContext.CorrelationId,
            StartTime = heartbeatEvent.ForwardSendTime,
            ReceivedTime = heartbeatEvent.ForwardReceivedTime,
            EndTime = DateTime.Now,
            EndpointHeartbeatStatus = HeartbeatStatus.On
        };
        await _cosmosDbClient.SetHeartbeat(heartbeatDto, heartbeatEvent.Endpoint);
        await PublishToStoragehook(heartbeatEvent.Endpoint);
        await messageContext.Complete();
    }

    private async Task PublishToStoragehook(string endpoint)
    {
        var baseUrl = Environment.GetEnvironmentVariable("StoragehookUrl");
        var url = $"{baseUrl}{endpoint}";

        var response = await _httpClient.PostAsync(url, null);
    }
}