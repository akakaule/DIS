using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace BH.DIS.WebApp.Services.ApplicationInsights
{
    public class ApplicationInsightsService : IApplicationInsightsService
    {

        private HttpClient client;

        public ApplicationInsightsService(string applicationId, string apiKey)
        {
            this.client = new HttpClient() { BaseAddress = new Uri($"https://api.applicationinsights.io/v1/apps/{applicationId}/") };
            this.client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        }

        public async Task<IEnumerable<LogEntry>> GetLogs(string messageId, SeverityLevel minimumLevel)
        {
            return await GetLogs(new Filter()
            {
                EventId = messageId,
                MinimumLogLevel = minimumLevel,
            });
        }

        public async Task<IEnumerable<LogEntry>> GetLogs(Filter filter)
        {
            var query = "traces " +
                    " | where itemType == 'trace' " +
                    (filter.Before == null ? "" : $"and timestamp <= datetime({filter.Before.Value.ToString("u")}) ") +
                    (filter.After == null ? "" : $"and timestamp >= datetime({filter.After.Value.ToString("u")}) ") +
                    //(filter.LogSource == null ? "" : $"and tostring(customDimensions['LogSource']) == '{filter.LogSource}' ") +
                    //(filter.EventType == null ? "" : $"and tostring(customDimensions['EventType']) == '{filter.EventType}' ") +
                    //(filter.CorrelationId == null ? "" : $"and tostring(customDimensions['CorrelationId']) == '{filter.CorrelationId}' ") +
                    (string.IsNullOrEmpty(filter.EventId) ? "" : $"and tostring(customDimensions['DIS.EventId']) == '{filter.EventId}' ") +
                    //(filter.PublishedBy == null ? "" : $"and tostring(customDimensions['PublishedBy']) == '{filter.PublishedBy.ToString()}' ") +
                    (filter.MinimumLogLevel == null ? "" : $"and severityLevel >= {(int)filter.MinimumLogLevel} ") +
                    " | top 1000 by timestamp desc";
            var req = $"query?query={HttpUtility.UrlEncode(query)}";

            using var response = await client.GetAsync(req);
            
            var result = await response.Content.ReadAsStringAsync();
            var obj = JsonConvert.DeserializeObject<AppInsightsResultRaw>(result);

            return new LogTraceCollection(obj).GetLogEntries();
        }
    }

    public interface IApplicationInsightsService
    {
        Task<IEnumerable<LogEntry>> GetLogs(string messageId, SeverityLevel minimumLevel = SeverityLevel.Information);
        Task<IEnumerable<LogEntry>> GetLogs(Filter filter);
    }
}
