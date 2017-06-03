using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MotionTelegramConnector.Services;
using Telegram.Bot.Types;

namespace MotionTelegramConnector.Controllers
{
    [Route("api")]
    public class TelegramApiController : Controller
    {
        private readonly TelegramService _telegram;
        private readonly ILogger<TelegramApiController> _logger;
        private int _errorId = 0;

        public TelegramApiController(TelegramService telegram, ILogger<TelegramApiController> logger)
        {
            _telegram = telegram;
            _logger = logger;
        }
        
        public async Task<ActionResult> Post()
        {
            Update update;
            try
            {
                var data = new StreamReader(HttpContext.Request.Body).ReadToEnd();
                update = Update.FromString(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(new EventId(_errorId++), ex, "Error on TelegramApiController");
                return Ok();
            }

            await _telegram.Process(update);
            return Ok();
        }
    }
}