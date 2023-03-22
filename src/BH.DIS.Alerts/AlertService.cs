using BH.DIS.Core.Logging;
using BH.DIS.MessageStore;
using BH.DIS.MessageStore.States;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using BH.DIS.Core;
using SendGrid;
using Microsoft.Extensions.Configuration;
using Azure.Messaging.EventGrid;
using BH.DIS.Alerts;
using Microsoft.Azure.Documents;

namespace BH.DIS.Alerts
{

    public interface IAlertService
    {
        void Run(Microsoft.Extensions.Logging.ILogger log);
        void RunAlertService(Microsoft.Extensions.Logging.ILogger log, QueueEvent eqEvent);
        void Subscribe(string endpoint, string mail, string severity, string type,string author, string url);
    }
    public class AlertService : IAlertService
    {
        private readonly string _logicAppUrl;
        private readonly string _sendGridEmail;
        private readonly string _sendGridTemplateId;
        private readonly string _webappLink;
        private readonly bool _useSendgridTemplate;

        private readonly ICosmosDbClient _client;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IPlatform _platform;
        private readonly SendGridClient _sendGridclient;
        private readonly IConfigurationRoot config;

        private const int MaxPendingEvents = 20;
        private const int MaxDefferedEvents = 5;
        private const int DaysBetweenSameNotification = 7;


        public AlertService(ICosmosDbClient dbClient, ILogger log, PlatformConfiguration platform)
        {
            var configBuilder = new ConfigurationBuilder()
                .AddEnvironmentVariables();
            config = configBuilder.Build();

            _logger = log;
            _client = dbClient;
            _httpClient = new HttpClient();
            _platform = platform;
            _sendGridclient = new SendGridClient(config["sendGridApiKey"]);
            _sendGridEmail = config["sendGridEmail"];
            _sendGridTemplateId = config["sendGridTemplateId"];
            _useSendgridTemplate = bool.Parse(config["sendGridUseTemplateBool"]);
            _webappLink = config["WebAppUrl"];
            _logicAppUrl = config["LogicAppSubscriptionsUrl"];

        }

        public async void Subscribe(string endpoint, string mail, string severity, string type, string author, string url)
        {
            var s = await _client.SubscribeToEndpointNotification(endpoint, mail, type, author, url, new List<string>(), "", 9999999);

        }

        public async void RunAlertService(Microsoft.Extensions.Logging.ILogger log, QueueEvent eqEvent)
        {
            var endpoint = eqEvent.subject;
            var eventType = eqEvent.eventType;
            var objectId = eqEvent.data;

            //Get event by Id from client
            var unresolvedEvent = await _client.GetEventById(endpoint, objectId);

            //Failed status
            if (unresolvedEvent.ResolutionStatus == ResolutionStatus.Failed)
            {
                //Find subscribers for endpoint, eventtype and payload
                var subscribers = await _client.GetSubscriptionsOnEndpointWithEventtype(endpoint, 
                    eventType, unresolvedEvent?.MessageContent?.EventContent?.EventJson, 
                    unresolvedEvent?.MessageContent?.ErrorContent?.ErrorText);
                                
                foreach (var sub in subscribers)
                {
                    //Last notified at older than frequency?
                    if(ValidateFrequency(sub))
                    {
                        if (sub.Type.Equals("mail", StringComparison.OrdinalIgnoreCase))
                        {
                            await CallSendGridApi(sub);
                        }
                        else if (sub.Type.Equals("teams", StringComparison.OrdinalIgnoreCase))
                        {
                            await CallLogicAppForTeams(sub);
                        }

                        //Update subscription
                        await _client.UpdateSubscription(sub);
                    }                   
                }
            }            
        }

        public async void Run(Microsoft.Extensions.Logging.ILogger log)
        {
            var errorSeveritySubs = new List<EndpointSubscription>();
            var warningSeveritySubs = new List<EndpointSubscription>();

            var affectedEndpoints = await GetAffectedEndpoints();
            foreach (var endpoint in affectedEndpoints)
            {
                var subscribersForSingleEndpoint = await GetSubscribersForEndpoint(endpoint.id);
                foreach (var subscriber in subscribersForSingleEndpoint)
                {
                    if (endpoint.isError) { errorSeveritySubs.Add(subscriber); }
                    else { warningSeveritySubs.Add(subscriber); }
                }

                if (errorSeveritySubs.Count > 0)
                {
                    await IterateSubscribtions(errorSeveritySubs);                   
                }

                if (warningSeveritySubs.Count > 0)
                {
                    await IterateSubscribtions(warningSeveritySubs);                   
                }

                errorSeveritySubs = new List<EndpointSubscription>();
                warningSeveritySubs = new List<EndpointSubscription>();
            }
            _logger?.Verbose($"SUBSCRIPTION: Succesfully sent notifications to subscribers");
        }

        private async Task IterateSubscribtions(List<EndpointSubscription> endpointSubscriptions)
        {
            
            foreach (var subscription in endpointSubscriptions)
            {
                var shouldNotify = false;

                //New Errors?                
                if (await ValidateNewError(subscription))
                {                    
                    shouldNotify = true;
                }
                
                //Notified more than a week ago?
                if(ValidateLastNotification(subscription))
                {
                    shouldNotify = true;
                }

                if (shouldNotify)
                {
                    if (subscription.Type.Equals("mail", StringComparison.OrdinalIgnoreCase))
                    {
                        await CallSendGridApi(subscription);
                    }
                    else if (subscription.Type.Equals("teams", StringComparison.OrdinalIgnoreCase))
                    {
                        await CallLogicAppForTeams(subscription);
                    }

                    //Update subscription
                    await _client.UpdateSubscription(subscription);
                }
            }
        }

        private bool ValidateLastNotification(EndpointSubscription endpointSubscription)
        {
            if(!string.IsNullOrEmpty(endpointSubscription.NotifiedAt))
            {
                var notifyDate = DateTime.Parse(endpointSubscription.NotifiedAt);
                if (DateTime.UtcNow <= notifyDate.AddDays(DaysBetweenSameNotification))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ValidateFrequency(EndpointSubscription endpointSubscription)
        {
            if (!string.IsNullOrEmpty(endpointSubscription.NotifiedAt))
            {
                var notifyDate = DateTime.Parse(endpointSubscription.NotifiedAt);
                var frequency = DateTime.UtcNow - notifyDate;
                if (Convert.ToInt32(frequency.TotalSeconds) < endpointSubscription.Frequency) return false;
            }
            return true;
        }

        //Returns true, if any new errors occours
        private async Task<bool> ValidateNewError(EndpointSubscription endpointSubscription)
        {
            var errorString = await _client.GetEndpointErrorList(endpointSubscription.EndpointId);
            var newErrors = errorString.Split(';');
            if (!string.IsNullOrEmpty(endpointSubscription.ErrorList))
            {
                var oldErrors = endpointSubscription.ErrorList.Split(';');
                foreach (var oldError in oldErrors)
                {
                    newErrors = newErrors.Where(x => !x.Equals(oldError, StringComparison.OrdinalIgnoreCase)).ToArray();
                }

                if (newErrors.Length == 0)
                {
                    return false;
                }
            }

            return true;
        }

        private async Task CallLogicAppForTeams(EndpointSubscription subscription)
        {
            var body = JsonConvert.SerializeObject(LogicAppSubscriptionFromendpointSubscription(subscription));
            var response = await _httpClient.PostAsync(subscription.Url, new StringContent(body, Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger?.Error($"SUBSCRIPTION ERROR: LogicApp Error when sending notifications to subscription {subscription.Id}. Error: {error}");
                throw new Exception($"LogicApp error: {error}");
            }
        }


        //For test
        private async Task<string> CallLogicApp(IEnumerable<EndpointSubscription> subscriptions)
        {
            var body = subscriptions.ToList();
            var jsonToReturn = JsonConvert.SerializeObject(body);
            var response = await _httpClient.PostAsync(_logicAppUrl, new StringContent(jsonToReturn, Encoding.UTF8, "application/json"));

            return response.StatusCode.ToString();
        }

        private async Task HandleSendGridResponse(SendGrid.Response response)
        {

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.DeserializeResponseBodyAsync(response.Body);
                _logger?.Error($"SUBSCRIPTION ERROR: SendGrid Error when sending notifications to subscribers. Error: {error}");
                throw new Exception($"SendGrid error: {error}");
            }
        }

        private async Task CallSendGridApi(EndpointSubscription subscription)
        {
            var msg = new SendGrid.Helpers.Mail.SendGridMessage();
            msg.SetFrom(_sendGridEmail, "noreply@kemp-lauritzen.dk");
            msg.AddTo(subscription.Mail);

            if (subscription?.NotificationSeverity?.Split(';').Count() > 1) subscription.NotificationSeverity = "error";

            if (_useSendgridTemplate)
            {
                msg.TemplateId = _sendGridTemplateId;
                msg.AddContent("text/plain", $"Your endpoint: {subscription.EndpointId} is {subscription.NotificationSeverity?.ToUpper()} Affected.");
            }
            else
            {
                msg.Subject = $"DIS Endpoint Notfication";
                msg.AddContent("text/plain", $"Your endpoint: {subscription.EndpointId} is {subscription?.NotificationSeverity?.ToUpper()} Affected. \r\n" +
                    $"Please go to the management app for more details. \r\n\r\n\r\n" +
                    $"The management app can be found at {_webappLink}");
            }

            var response = await _sendGridclient.SendEmailAsync(msg);
            await HandleSendGridResponse(response);
        }

        private async Task<IEnumerable<AffectedEndpoint>> GetAffectedEndpoints()
        {
            var affectedEndpoints = new List<AffectedEndpoint>();

            var endpoints = _platform.Endpoints.ToList();
            var endpointStateCounts = new List<EndpointStateCount>();
            foreach (var endpoint in endpoints)
            {
                endpointStateCounts.Add(await _client.DownloadEndpointStateCount(endpoint.Id));
            }

            foreach (var endpointState in endpointStateCounts)
            {
                if (endpointState.FailedCount > 0)
                {
                    affectedEndpoints.Add(new AffectedEndpoint
                    {
                        id = endpointState.EndpointId,
                        isError = true
                    });
                    continue;
                }

                if (endpointState.DeferredCount >= MaxDefferedEvents)
                {
                    affectedEndpoints.Add(new AffectedEndpoint
                    {
                        id = endpointState.EndpointId,
                        isError = false
                    });
                    continue;
                }

                if (endpointState.PendingCount >= MaxPendingEvents)
                {
                    affectedEndpoints.Add(new AffectedEndpoint
                    {
                        id = endpointState.EndpointId,
                        isError = false
                    });
                }
            }

            return affectedEndpoints;
        }

        private async Task<IEnumerable<EndpointSubscription>> GetSubscribersForEndpoint(string endpoint)
        {
            return await _client.GetSubscriptionsOnEndpoint(endpoint);
        }

        private LogicAppSubscription LogicAppSubscriptionFromendpointSubscription(EndpointSubscription subscription)
        {
            if (subscription.NotificationSeverity.Split(';').Length > 1) 
                subscription.NotificationSeverity = "error";
            
            return new LogicAppSubscription
            {
                endpoint = subscription.EndpointId,
                author = subscription.AuthorId,
                webappLink = _webappLink,
                severity = subscription.NotificationSeverity
            };
        }
        
        public class AffectedEndpoint
        {
            public string id { get; set; }
            public bool isError { get; set; }
        }

        public class LogicAppSubscription
        {
            public string author { get; set; }
            public string endpoint { get; set; }
            public string webappLink { get; set; }
            public string severity { get; set; }
        }
    }
}
