using BH.DIS.Core;
using BH.DIS.Core.Endpoints;
using BH.DIS.Core.Events;
using BH.DIS.WebApp.ManagementApi;
using BH.DIS.WebApp.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BH.DIS.WebApp.Controllers.ApiContract
{
    public class EventTypeImplementation : IEventTypeApiController
    {
        private readonly IPlatform platform;
        private readonly ICodeRepoService codeRepoService;

        public EventTypeImplementation(IPlatform platform, ICodeRepoService codeRepoService)
        {
            this.platform = platform;
            this.codeRepoService = codeRepoService;
        }

        public async Task<ActionResult<IEnumerable<ManagementApi.EventType>>> GetEventTypesAsync()
        {
            var eventTypes = platform.EventTypes.Select(e => Mapper.EventTypeFromIEventType(e));
            return new OkObjectResult(eventTypes);
        }

        public async Task<ActionResult<Response>> GetEventtypesByEndpointIdAsync(string endpointId)
        {
            IEndpoint endpoint = platform.Endpoints.FirstOrDefault(e => e.Name.Equals(endpointId, StringComparison.OrdinalIgnoreCase));
            if (endpoint == null)
            {
                return new NotFoundObjectResult("Endpoint not found");
            }

            var eventTypeDetails = new List<EventTypeDetails>();

            foreach(var eventType in endpoint.EventTypesConsumed)
            {
                eventTypeDetails.Add(new EventTypeDetails
                {
                    EventType = Mapper.EventTypeFromIEventType(eventType),
                    CodeRepoLink = codeRepoService.GetSearchUrl(eventType.Name, eventType.Namespace),
                    Producers = platform.GetProducers(eventType).Select(x => x.Name).ToList(),
                    Consumers = platform.GetConsumers(eventType).Select(x => x.Name).ToList(),
                });
            }

            foreach (var eventType in endpoint.EventTypesProduced)
            {
                eventTypeDetails.Add(new EventTypeDetails
                {
                    EventType = Mapper.EventTypeFromIEventType(eventType),
                    CodeRepoLink = codeRepoService.GetSearchUrl(eventType.Name, eventType.Namespace),
                    Producers = platform.GetProducers(eventType).Select(x => x.Name).ToList(),
                    Consumers = platform.GetConsumers(eventType).Select(x => x.Name).ToList(),
                });
            }
            
            return new Response
            {
                Consumes = endpoint.EventTypesConsumed.Select(Mapper.EventTypeFromIEventType).GroupBy(e => e.Namespace).Select(g => new EventTypeGrouping() { Namespace = g.Key, Events = g.ToList() }).ToList(),
                Produces = endpoint.EventTypesProduced.Select(Mapper.EventTypeFromIEventType).GroupBy(e => e.Namespace).Select(g => new EventTypeGrouping() { Namespace = g.Key, Events = g.ToList() }).ToList(),
                EventTypeDetails = eventTypeDetails
            };
        }

        public async Task<ActionResult<EventTypeDetails>> GetEventtypesEventtypeidAsync(string eventtypeid)
        {
            var eventType = platform.EventTypes.Single(et => et.Id.Equals(eventtypeid, StringComparison.OrdinalIgnoreCase));
            if (eventType != null)
            {
                var eventTypeDetails = new EventTypeDetails
                {
                    EventType = Mapper.EventTypeFromIEventType(eventType),
                    CodeRepoLink = codeRepoService.GetSearchUrl(eventType.Name, eventType.Namespace),
                    Producers = platform.GetProducers(eventType).Select(x => x.Name).ToList(),
                    Consumers = platform.GetConsumers(eventType).Select(x => x.Name).ToList(),
                };
                return new OkObjectResult(eventTypeDetails);
            }

            return new NotFoundObjectResult("EventType not found");
        }
    }
}
