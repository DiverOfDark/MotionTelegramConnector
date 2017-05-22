using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MotionTelegramConnector.MotionAi
{
    public class MotionAiService
    {
        private static readonly Regex NextMessageRegex = new Regex(@"(?<next>::next-?(?<timespan>[\d]+)?::)", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex ImgRegex = new Regex(@"\[img](?<url>.*)\[\/img]", RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly HttpClient HttpClient = new HttpClient();

        private readonly ITelegramBotClient _client;
        private readonly string _botId;
        private readonly string _apiKey;

        public MotionAiService(string apiKey, string botId, ITelegramBotClient client)
        {
            _apiKey = apiKey;
            _botId = botId;
            _client = client;
        }

        private string GetUrl(string message, string session) =>
            $"https://api.motion.ai/messageBot?msg={UrlEncoder.Default.Encode(message)}&bot={_botId}&session={session}&key={_apiKey}";

        public async Task<string> SendRequest(string message, string session, Action<Exception> log)
        {
            var data = await Extensions.Retry(()=> HttpClient.GetStringAsync(GetUrl(message, session)));

            Process(session, data, log);

            return data;
        }

        private async void Process(string session, string data, Action<Exception> log)
        {
            var jobject = JsonConvert.DeserializeObject<Response>(data);

            if (jobject.BotResponse != null)
            {
                IReplyMarkup markup = null;

                if (jobject.QuickReplies?.Any() == true)
                {
                    markup = new ReplyKeyboardMarkup(
                        jobject.QuickReplies.Select(v => new[] {new KeyboardButton(v.Title)}).ToArray(),
                        oneTimeKeyboard: true);
                }

                var responses = new List<object> {jobject.BotResponse};
                var splitByMessages = NextMessageRegex.Matches(jobject.BotResponse);

                if (splitByMessages.Count > 0)
                {
                    responses.Clear();
                    int pos = 0;
                    foreach (Match item in splitByMessages)
                    {
                        responses.Add(jobject.BotResponse.Substring(pos, item.Index - pos));
                        pos = item.Index + item.Length;

                        try
                        {
                            if (item.Groups["timespan"] != null)
                            {
                                var timespanValue = item.Groups["timespan"].Value;
                                responses.Add(Convert.ToInt32(timespanValue));
                            }
                        }
                        catch(Exception ex)
                        {
                            log(ex);
                        }
                    }
                    responses.Add(jobject.BotResponse.Substring(pos));
                }

                var newResponses = new List<object>();

                for (var index = 0; index < responses.Count; index++)
                {

                    var item = responses[index] as string;
                    if (item != null)
                    {
                        var matches = ImgRegex.Matches(item);
                        foreach (Match m in matches)
                        {
                            var url = m.Groups["url"].Value;

                            newResponses.Add(new Uri(url));

                            item = item.Replace(m.Value, "");
                        }
                    }
                    newResponses.Add(item ?? responses[index]);
                }

                responses = newResponses;

                for (var i = 0; i < responses.Count; i++)
                {
                    var item = responses[i];
                    if (!string.IsNullOrWhiteSpace(item as string))
                    {
                        await Extensions.Retry(()=>_client.SendTextMessageAsync(session, (string) item,
                            replyMarkup: item == responses.Last() ? markup : null));
                    }
                    else if (item is Uri)
                    {
                        await Extensions.Retry(()=>_client.SendPhotoAsync(session, new FileToSend((Uri) item)));
                    }
                    else if (item is int)
                    {
                        var nextItemIsPhoto = responses[i + 1] is Uri;
                        await Extensions.Retry(()=>_client.SendChatActionAsync(session, nextItemIsPhoto ? ChatAction.UploadPhoto : ChatAction.Typing));
                        await Task.Delay((int) item);
                    }
                }
            }
        }
    }
}