using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BH.DIS.Core;
using BH.DIS.Core.Events;
using BH.DIS.WebApp.Models;
using BH.DIS.WebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace BH.DIS.WebApp.Controllers
{
    [Route("EventTypes")]
    public class EventTypesController : Controller
    {
        private readonly IPlatform _platform;
        private readonly ICodeRepoService _codeRepoService;

        public EventTypesController(IPlatform platform, ICodeRepoService codeRepoService)
        {
            _platform = platform;
            _codeRepoService = codeRepoService;
        }

        [Route("/EventTypes")]
        public IActionResult Index()
        {
            IEnumerable<IEventType> model = _platform.EventTypes;
            return base.View(model);
        }

        [Route("/EventTypes/Details/{id}")]
        public IActionResult Details(string id)
        {
            var eventType = _platform.EventTypes.Single(et => et.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            var exampleEvent = JsonConvert.SerializeObject(eventType.GetEventExample(), Formatting.Indented);

            var model = new EventTypeViewModel
            {
                EventType = eventType,
                CodeRepoLink = _codeRepoService.GetSearchUrl(eventType.Name, eventType.Namespace),
                Producers = _platform.GetProducers(eventType),
                Consumers = _platform.GetConsumers(eventType),
                ExampleEventJson = !string.IsNullOrEmpty(exampleEvent) && !exampleEvent.Equals("null", StringComparison.OrdinalIgnoreCase) ? exampleEvent : "No example available"
            };

            return View(model);
        }
    }
}
