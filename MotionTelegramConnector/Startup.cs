using System;
using System.IO;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MotionTelegramConnector.MotionAi;
using Telegram.Bot;

namespace MotionTelegramConnector
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; set; }

        public Startup()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var settings = Configuration.Get<AppSettings>();

            settings.Validate();
            
            var client = new TelegramBotClient(settings.TELEGRAM_API_KEY,
                new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(15)
                });
            client.SetWebhookAsync(settings.WEBSITE_URL);

            var mai = new MotionAiService(settings.MOTION_API_KEY, settings.MOTION_BOT_ID, client);
            
            services.AddSingleton<ITelegramBotClient>(client);
            services.AddSingleton<MotionAiService>(mai);
            
            services.AddMvc();
            services.AddWebEncoders();
            services.AddRouting();
            services.AddLogging();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc(routes =>
            {
                routes.MapRoute("Default",
                    "{controller}/{action}/{id?}",
                    new
                    {
                        controller = "Home",
                        action = "Index",
                        area = ""
                    });
            });
        }
    }
}
