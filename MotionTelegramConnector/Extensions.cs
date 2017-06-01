using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Logging;
using MotionTelegramConnector.Controllers;
using MotionTelegramConnector.MotionAi;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MotionTelegramConnector
{
    public static class Extensions
    {
        private static readonly List<string> DebugSessions = new List<string>();

        public static async void Process(Update update, ILogger<ApiController> _logger, ITelegramBotClient _client, MotionAiService _svc)
        {
            if (update.Message.Text == "/switchDebug")
            {
                if (!DebugSessions.Contains(update.Message.Chat.Id))
                {
                    DebugSessions.Add(update.Message.Chat.Id);
                }
                else
                {
                    DebugSessions.Remove(update.Message.Chat.Id);
                }
            }

            _logger.LogInformation(JsonConvert.SerializeObject(update));
            var debug = DebugSessions.Contains(update.Message.Chat.Id);

            await Retry(()=> _client.SendChatActionAsync(update.Message.Chat.Id, ChatAction.Typing));

            try
            {
                var message = update.Message;

                var response = await _svc.SendRequest(update.Message.Text, message.Chat.Id, _logger);

                if (debug)
                {
                    // Echo each Message
                    await Retry(()=> _client.SendTextMessageAsync(message.Chat.Id, response));
                    await Retry(()=> _client.SendTextMessageAsync(message.Chat.Id, JsonConvert.SerializeObject(message)));
                }
            }
            catch (Exception ex) when (debug)
            {
                await Retry(()=> _client.SendTextMessageAsync(update.Message.Chat.Id, ex.ToString()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }
        
        public static async Task Retry(this Func<Task> action)
        {
            int i = 0;
            while(true)
            {
                i++;
                try
                {
                    await action();
                    break;
                }
                catch
                {
                    if (i > 3)
                    {
                        throw;
                    }
                }
            }
        }

        public static async Task<TResult> Retry<TResult>(this Func<Task<TResult>> action)
        {
            int i = 0;
            TResult result;
            while(true)
            {
                i++;
                try
                {
                    result = await action();
                    break;
                }
                catch
                {
                    if (i > 3)
                    {
                        throw;
                    }
                }
            }
            return result;
        }
    }
}