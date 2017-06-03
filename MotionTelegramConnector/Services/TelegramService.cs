using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MotionTelegramConnector.Services
{
    public class TelegramService
    {
        private readonly AppSettings _settings;
        private readonly ILogger<TelegramService> _logger;
        private readonly ITelegramBotClient _client;
        private readonly MotionAiService _svc;
        private static readonly List<ChatId> DebugSessions = new List<ChatId>();
        private static Timer _timer;

        public TelegramService(AppSettings settings, ILogger<TelegramService> logger, ITelegramBotClient client, MotionAiService svc)
        {
            _settings = settings;
            _logger = logger;
            _client = client;
            _svc = svc;
        }
        
        public async void Init()
        {
            if (!string.IsNullOrWhiteSpace(_settings.WEBSITE_URL))
            {
                await _client.SetWebhookAsync(_settings.WEBSITE_URL);
            }
            else
            {
                var whi = await _client.GetWebhookInfoAsync();
                Console.WriteLine(JsonConvert.SerializeObject(whi));

                if (!string.IsNullOrWhiteSpace(whi.Url))
                {
                    await _client.DeleteWebhookAsync();
                }

                int lastId = -1; 
                
                _timer = new Timer(async _ =>
                {
                    var updates = await _client.GetUpdatesAsync(lastId + 1);
                    lastId = updates.FirstOrDefault()?.Id ?? lastId;
                    foreach (var up in updates)
                    {
                        await Process(up);
                    }
                }, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
            }
        }
        
        public async Task SendToDebug(string data)
        {
            foreach (var chatid in DebugSessions)
            {
                await _client.SendTextMessageAsync(chatid, data);
            }
        }

        public async Task Process(Update update)
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

            await Extensions.Retry(()=> _client.SendChatActionAsync(update.Message.Chat.Id, ChatAction.Typing));

            try
            {
                var message = update.Message;

                var response = await _svc.SendRequest(update.Message.Text, message.Chat.Id);

                await SendToDebug(response);
                await SendToDebug(JsonConvert.SerializeObject(message));
            }
            catch (Exception ex)
            {
                await SendToDebug(ex.ToString());
                _logger.LogError(ex.ToString());
            }
        }
    }
}