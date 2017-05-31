using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MotionTelegramConnector.Controllers;
using MotionTelegramConnector.MotionAi;
using Newtonsoft.Json;
using Telegram.Bot;

namespace MotionTelegramConnector
{
    public class Startup
    {
        private static Timer _timer;
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

          var mai = new MotionAiService(settings.MOTION_API_KEY, settings.MOTION_BOT_ID, client);
            
            services.AddSingleton<ITelegramBotClient>(client);
            services.AddSingleton<MotionAiService>(mai);
            
            services.AddMvc();
            services.AddWebEncoders();
            services.AddRouting();
            services.AddLogging();
            
            Init(settings, client, mai);
        }

        private static async void Init(AppSettings settings, TelegramBotClient client, MotionAiService mai)
        {
            if (!string.IsNullOrWhiteSpace(settings.WEBSITE_URL))
            {
                await client.SetWebhookAsync(settings.WEBSITE_URL);
            }
            else
            {
                var whi = await client.GetWebhookInfoAsync();
                Console.WriteLine(JsonConvert.SerializeObject(whi));

                if (!string.IsNullOrWhiteSpace(whi.Url))
                {
                    await client.DeleteWebhookAsync();
                }

                var apiController = new ApiController(new Logger<ApiController>(new LoggerFactory()), mai, client);

                int lastId = -1; 
                
                _timer = new Timer(async _ =>
                {
                    var updates = await client.GetUpdatesAsync(lastId + 1);
                    lastId = updates.FirstOrDefault()?.Id ?? lastId;
                    foreach (var up in updates)
                    {
                        apiController.Process(up);
                    }
                }, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Debug);

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
