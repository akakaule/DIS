using BH.DIS.Core;
using BH.DIS.Core.Endpoints;
using BH.DIS.Core.Events;
using BH.DIS.MessageStore;
using BH.DIS.WebApp.Actions;
using BH.DIS.WebApp.ManagementApi;
using BH.DIS.WebApp.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BH.DIS.WebApp.Controllers
{
    public class MonitorController : Controller
    {
        private readonly IPlatform platform;
        private readonly IMessageStoreClient messageStoreClient;
        public MonitorController(IMessageStoreClient messageStoreClient, IPlatform platform)
        {
            this.messageStoreClient = messageStoreClient;
            this.platform = platform;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
