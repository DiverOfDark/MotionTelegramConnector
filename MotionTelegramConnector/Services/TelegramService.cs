using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
                    try
                    {
                        var updates = await _client.GetUpdatesAsync(lastId + 1);
                        lastId = updates.LastOrDefault()?.Id ?? lastId;
                        foreach (var up in updates)
                        {
                            try
                            {
                                await Process(up);
                            }
                            catch { }
                        }
                    }
                    catch { }
                }, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
            }
        }

        public async void SendToDebug(string data, [CallerMemberName] string method = null, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            foreach (var chatid in DebugSessions)
            {
                var message = $"{method} ({file} at {line}):\n\n{data}";
                await _client.SendTextMessageAsync(chatid, message);
            }
        }

        public async Task Process(Update update)
        {
            if (update.Message.Text == "/switchDebug")
            {
                var existing = DebugSessions.FirstOrDefault(v => v.Identifier == update.Message.Chat.Id);
                if (existing != null)
                {
                    DebugSessions.Remove(existing);
                }
                else
                {
                    DebugSessions.Add(update.Message.Chat.Id);
                }
            }

            _logger.LogInformation(JsonConvert.SerializeObject(update));

            await Extensions.Retry(() => _client.SendChatActionAsync(update.Message.Chat.Id, ChatAction.Typing));

            try
            {
                var message = update.Message;

                var response = await _svc.SendRequest(update.Message.Text, message.Chat.Id.ToString());

                SendToDebug(response + "\r\n" + JsonConvert.SerializeObject(message));
            }
            catch (Exception ex)
            {
                SendToDebug(ex.ToString());
                _logger.LogError(ex.ToString());
            }
        }
    }
}