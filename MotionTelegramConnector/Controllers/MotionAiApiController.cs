using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MotionTelegramConnector.Services;
using Telegram.Bot;

namespace MotionTelegramConnector.Controllers
{
    [Route("mai-api")]
    public class MotionAiApiController : Controller
    {
        private readonly TelegramService _client;

        public MotionAiApiController(TelegramService client)
        {
            _client = client;
        }
        
        public async Task<ActionResult> Post()
        {
            try
            {
                var data = new StreamReader(HttpContext.Request.Body).ReadToEnd();

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