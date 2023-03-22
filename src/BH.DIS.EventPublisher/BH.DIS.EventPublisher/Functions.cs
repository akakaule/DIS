using BH.DIS.EventPublisher.Models;
using BH.DIS.EventPublisher.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace BH.DIS.EventPublisher
{
    public class Functions
    {
        private readonly IEventPublisherService _eventPublisherService;

        public Functions(IEventPublisherService eventPublisherService)
        {
            _eventPublisherService = eventPublisherService;
        }

        [FunctionName("EventPublisher")]
        public async Task RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var eventModel = JsonConvert.DeserializeObject<EventPublisherModel>(requestBody);

            await _eventPublisherService.Handle(eventModel);

        }
    }
}
