using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MotionTelegramConnector.MotionAi;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MotionTelegramConnector.Controllers
{
    [Route("api")]
    public class ApiController : Controller
    {
        private readonly ILogger<ApiController> _logger;
        private readonly MotionAiService _svc;
        private readonly ITelegramBotClient _client;
        private readonly List<string> _debugSessions = new List<string>();
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

            if (update.Message.Text == "/switchDebug")
            {
                if (!_debugSessions.Contains(update.Message.Chat.Id))
                {
                    _debugSessions.Add(update.Message.Chat.Id);
                }
                else
                {
                    _debugSessions.Remove(update.Message.Chat.Id);
                }
            }

            Process(update);
            return Ok();
        }

        private async void Process(Update update)
        {
            var debug = _debugSessions.Contains(update.Message.Chat.Id);

            await Extensions.Retry(()=> _client.SendChatActionAsync(update.Message.Chat.Id, ChatAction.Typing));

            try
            {
                var message = update.Message;

                var response = await _svc.SendRequest(update.Message.Text, message.Chat.Id, LogEx);

                if (debug)
                {
                    // Echo each Message
                    await Extensions.Retry(()=> _client.SendTextMessageAsync(message.Chat.Id, response));
                }
            }
            catch (Exception ex) when (debug)
            {
                await Extensions.Retry(()=> _client.SendTextMessageAsync(update.Message.Chat.Id, ex.ToString()));
            }
            catch (Exception ex)
            {
                LogEx(ex);
            }
        }

        private void LogEx(Exception ex) => _logger.LogError(new EventId(_errorId++), ex, "Error on ApiController");
    }
}