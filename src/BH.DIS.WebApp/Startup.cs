using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BH.DIS.WebApp.Hubs;
using BH.DIS.WebApp.Services.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using NSwag.AspNetCore;
using BH.DIS.Core;
using BH.DIS.Core.Logging;
using BH.DIS.Core.Messages;
using BH.DIS.Manager;
using BH.DIS.MessageStore;
using BH.DIS.ServiceBus;
using BH.DIS.WebApp.Services;
using BH.DIS.WebApp.ManagementApi;
using BH.DIS.WebApp.Controllers;
using System.Linq;
using BH.DIS.WebApp.Controllers.ApiContract;
using System.Text.Json.Serialization;
using Microsoft.Azure.Cosmos;
using Serilog;
using BH.DIS.SDK.Logging;
using BH.DIS.Management.ServiceBus;
using Azure.Messaging.ServiceBus;

namespace BH.DIS.WebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment webEnv)
        {
            Configuration = configuration;
            Env = webEnv;
        }

        public IWebHostEnvironment Env { get; }
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services
            .AddAuthentication("Az")
            .AddPolicyScheme("Az", "Authorize AzureAD or AzureADBearer", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                    if (authHeader?.StartsWith("Bearer") == true)
                    {
                        return AzureADDefaults.JwtBearerAuthenticationScheme;
                    }
                    return AzureADDefaults.AuthenticationScheme;
                };
            })
            .AddAzureADBearer(options => Configuration.Bind("AzureAd", options))
            .AddAzureAD(options => Configuration.Bind("AzureAd", options));

            services.AddControllersWithViews(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            });

            services.AddControllers().AddJsonOptions(opts =>
            {
                opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            services.AddMvc().AddRazorRuntimeCompilation();

            services.AddSwaggerDocument(s =>
            {
                s.Title = "DIS Management API";
                s.Description = "Enterprise Integration Platform API - For data displayed in the management-webapp.";
            });

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build/public";
            });

            services.AddSignalR();

            services.AddSingleton<IPlatform, PlatformConfiguration>();

            string serviceBusConnection = Configuration.GetValue<string>("AzureWebJobsServiceBus");
            services.AddSingleton<ISender>(sp => new SenderManager(new ServiceBusClient(serviceBusConnection).CreateSender(BH.DIS.Core.Messages.Constants.ManagerId)));

            string globalTraceLogInstrKey = Configuration.GetValue<string>("APPINSIGHTS_INSTRUMENTATIONKEY");
            services.AddSingleton<ILoggerProvider>(sp => { 
                Serilog.ILogger baseLogger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .WriteTo.ApplicationInsights(globalTraceLogInstrKey, TelemetryConverter.Traces)
                .CreateLogger();

                return new LoggerProvider(baseLogger);
            });

            string storageConnection = Configuration.GetValue<string>("MessageStoreStorageConnection");
            services.AddSingleton<IMessageStoreClient>(sp => new MessageStoreClient(storageConnection));

            string cosmosConnection = Configuration.GetValue<string>("CosmosConnection");
            services.AddSingleton<ICosmosDbClient>(sp => new CosmosDbClient(new CosmosClient(cosmosConnection)));
            
            services.AddSingleton<IManagerClient, ManagerClient>();

            services.AddSingleton<ICodeRepoService>(sp => new CodeRepoService(Configuration["RepositoryUrl"]));

            services.AddSingleton<IServiceBusManagement>(sp => new ServiceBusManagement(new Azure.Messaging.ServiceBus.Administration.ServiceBusAdministrationClient(serviceBusConnection)));

            services.AddSingleton<IApplicationInsightsService>(services =>
                new ApplicationInsightsService(Configuration.GetValue<string>("AppInsights:ApplicationId"), Configuration.GetValue<string>("AppInsights:ApiKey"))
            );

            services.AddApplicationInsightsTelemetry();
            services.AddTransient<IEndpointApiController, EndpointImplementation>();
            services.AddTransient<IStorageHookApiController, StorageHookImplementation>();
            services.AddTransient<IEventApiController, EventImplementation>();
            services.AddTransient<IEventTypeApiController, EventTypeImplementation>();
            services.AddTransient<IApplicationApiController, ApplicationImplementation>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseCors(o =>
            {
                o.AllowCredentials().AllowAnyHeader().AllowAnyMethod().WithOrigins("login.microsoftonline.com").Build();
            });

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseDeveloperExceptionPage();

            app.UseSpaStaticFiles(new StaticFileOptions
            {
                //OnPrepareResponse = ctx =>
                //{
                //    if(ctx.File.Name.Contains("index.html", System.StringComparison.OrdinalIgnoreCase))
                //    {
                //        if (!ctx.Context.User.Identity.IsAuthenticated)
                //        {
                //            // Can redirect to any URL where you prefer.
                //            ctx.Context.Response.Redirect("/login");
                //        }
                //    }
                //    else
                //    {
                //        ctx.Context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                //        // Append following 2 lines to drop body from static files middleware!
                //        ctx.Context.Response.ContentLength = 0;
                //        ctx.Context.Response.Body = Stream.Null;
                //    }

                //}
            });
            
            app.UseOpenApi();
            app.UseSwaggerUi3();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<GridEventsHub>(Constants.AppEndpoints.GridEventHub);
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html", new StaticFileOptions
                {
                    OnPrepareResponse = ctx =>
                    {
                        if (!ctx.Context.User.Identity.IsAuthenticated)
                        {
                            // Can redirect to any URL where you prefer.
                            ctx.Context.Response.Redirect("/login");
                        }
                    }
                });
            });
        }
    }
}
