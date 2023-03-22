using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BH.DIS.Core;
using BH.DIS.Core.Endpoints;
using BH.DIS.Core.Events;
using BH.DIS.Core.Messages;
using BH.DIS.Endpoints.Demo;
using BH.DIS.Manager;
using BH.DIS.MessageStore;
using BH.DIS.WebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using BH.DIS.WebApp.Actions;
using BH.DIS.WebApp.ManagementApi;

namespace BH.DIS.WebApp.Controllers
{
    public class EndpointsController : Controller
    {
        private readonly IPlatform platform;
        private readonly IMessageStoreClient messageStoreClient;
        private readonly IManagerClient managerClient;
        private readonly IConfiguration configuration;
        public EndpointsController(IPlatform platform, IMessageStoreClient messageStoreClient, IManagerClient managerClient, IConfiguration configuration)
        {
            this.platform = platform;
            this.messageStoreClient = messageStoreClient;
            this.managerClient = managerClient;
            this.configuration = configuration;
        }

    }
}
