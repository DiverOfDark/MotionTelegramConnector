using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MotionTelegramConnector.MotionAi;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MotionTelegramConnector.Controllers
{
    [Route("api")]
    public class ApiController : Controller
    {
        private readonly ILogger<ApiController> _logger;
        private readonly MotionAiService _svc;
        private readonly ITelegramBotClient _client;
        private int _errorId = 0;

        public ApiController(ILogger<ApiController> logger, MotionAiService service, ITelegramBotClient client)
        {
            _logger = logger;
            _svc = service;
            _client = client;
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
                _logger.LogError(new EventId(_errorId++), ex, "Error on ApiController");
                return Ok();
            }

            Extensions.Process(update, _logger, _client, _svc);
            return Ok();
        }
    }
}