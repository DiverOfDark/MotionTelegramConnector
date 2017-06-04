using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MotionTelegramConnector.Services;

namespace MotionTelegramConnector.Controllers
{
    [Route("mai-api")]
    public class MotionAiApiController : Controller
    {
        private readonly TelegramService _client;
        private readonly GoogleAnalyticsService _ga;

        public MotionAiApiController(TelegramService client, GoogleAnalyticsService ga)
        {
            _client = client;
            _ga = ga;
        }

        [Route("event/{eventName}")]
        public async Task<ActionResult> Event(string eventName)
        {
            try
            {
                var data = new StreamReader(HttpContext.Request.Body).ReadToEnd();

                var entries = data.Split('&');

                var keyValuePairs = entries.Select(v =>
                {
                    var items = v.Split('=');
                    return new KeyValuePair<string, string>(items[0], WebUtility.UrlDecode(items[1]));
                });

                var session = keyValuePairs.FirstOrDefault(v => v.Key == "session").Value;

                _ga.LogEvent(eventName, session);
                
                await _client.SendToDebug(data);
            }
            catch (Exception ex)
            {
                await _client.SendToDebug(ex.ToString());
                return Ok();
            }

            return Ok();
        }
        
        public async Task<ActionResult> Post()
        {
            try
            {
                var data = new StreamReader(HttpContext.Request.Body).ReadToEnd();

                var entries = data.Split('&');

                var keyValuePairs = entries.Select(v =>
                {
                    var items = v.Split('=');
                    return new KeyValuePair<string, string>(items[0], WebUtility.UrlDecode(items[1]));
                }).ToList();

                var moduleName = keyValuePairs.First(v => v.Key == "moduleNickname").Value;
                var session = keyValuePairs.First(v => v.Key == "session").Value;

                if (!string.IsNullOrWhiteSpace(moduleName) && !string.IsNullOrWhiteSpace(session))
                {
                    _ga.LogPageView(moduleName, session);
                }
                await _client.SendToDebug(data);
            }
            catch (Exception ex)
            {
                await _client.SendToDebug(ex.ToString());
                return Ok();
            }

            return Ok();
        }
    }
}