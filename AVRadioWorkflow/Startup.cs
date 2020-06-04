using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MassTransit;
using MassTransit.RabbitMqTransport.Topology;

namespace AVRadioWorkflow
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            /*
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            */

            var mqConfig = Configuration.GetSection("rabbitMQ").Get<neSchedular.RabbitConfig>();
            if(null == mqConfig)
            {
                throw new Exception("RabbitMQ not configured");
            }

            services.AddMassTransit(x =>
            {

                x.AddBus(context => Bus.Factory.CreateUsingRabbitMq(cfg =>
                {

                    cfg.PublishTopology.BrokerTopologyOptions = PublishBrokerTopologyOptions.MaintainHierarchy;

                    //will add healt checks later
                    //https://masstransit-project.com/usage/configuration.html#asp-net-core
                    // configure health checks for this bus instance
                    //cfg.UseHealthCheck(context);

                    cfg.Host(mqConfig.hostname, h => {
                        h.Username(mqConfig.user);
                        h.Password(mqConfig.pass);
                    });
                }));
            });

            EndpointConvention.Map<neSchedular.ExecuteJobTask>(
                new Uri($"rabbitmq://{mqConfig.hostname}/{neSchedular.ExecuteJobTask.Q_NAME}"));

            services.AddTransient<components.mediaList.IStorageService, components.mediaList.StorageService>();

            services.AddControllersWithViews().AddNewtonsoftJson();
            services.AddRazorPages().AddNewtonsoftJson();


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            

            app.UseExceptionHandler(
             builder =>
             {
                 builder.Run(
                   async context =>
                   {

                       var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

                       //internal aplication DONOT hide error
                       var error = bootCommon.ErrorMessage.SetStatusGetResult(context, exception, loggerFactory.CreateLogger("Global-Exception"));
                       context.Response.ContentType = "application/json";

                       await context.Response.WriteAsync(JsonConvert.SerializeObject(error)).ConfigureAwait(false);
                   });
             });

            app.UseStaticFiles();
            //app.UseCookiePolicy();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapFallbackToController("Index", "Home");
            });

            
        }
    }
}
