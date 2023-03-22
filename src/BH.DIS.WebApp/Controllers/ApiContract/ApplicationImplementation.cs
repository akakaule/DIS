using BH.DIS.WebApp.ManagementApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace BH.DIS.WebApp.Controllers.ApiContract
{

    namespace BH.DIS.WebApp.ManagementApi
    {
        [AllowAnonymous]
        public partial class ApplicationApiController : Controller { }
    }

    public class ApplicationImplementation : IApplicationApiController
    {
        private readonly IConfiguration _config;

        public ApplicationImplementation(IConfiguration config)
        {
            _config = config;
        }

        public async Task<ActionResult<ApplicationStatus>> GetApiAppStatsAsync()
        {
            var platformVersion = "TBD";
            var bhAssembly = Assembly.GetAssembly(typeof(PlatformConfiguration));
            if (bhAssembly != null)
            {
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(bhAssembly.Location);
                var productVersion = fileVersionInfo.ProductVersion;
                platformVersion = productVersion?.Split("+")[0];
            }
            
            var statusResponse = new ApplicationStatus()
            {
                Env = _config.GetValue<string>("Environment"),
                
                PlatformVersion = platformVersion
            };

            return new OkObjectResult(statusResponse);
        }
    }
}
