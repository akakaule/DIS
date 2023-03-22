using BH.DIS.Core;
using BH.DIS.Core.Endpoints;
using BH.DIS.Core.Events;
using BH.DIS.WebApp.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BH.DIS.WebApp.Controllers
{
    public class DiagnosticsController : Controller
    {
        public IActionResult Index()
        {
            return View("EventGridViewer");
        }
    }
}
