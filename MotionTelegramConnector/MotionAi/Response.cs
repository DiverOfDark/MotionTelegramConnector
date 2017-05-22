using System.Collections.Generic;
using Newtonsoft.Json;

namespace MotionTelegramConnector.MotionAi
{
    [JsonObject]
    public class Response
    {
        [JsonProperty("botResponse")]
        public string BotResponse { get; set; }

        [JsonProperty("result")]
        public string Result { get; set; }

        [JsonProperty("enrichedData")]
        public object EnrichedData { get; set; }

        [JsonProperty("inReplyTo")]
        public string InReplyTo { get; set; }

        [JsonProperty("module")]
        public int Module { get; set; }

        [JsonProperty("cards")]
        public object Cards { get; set; }

        [JsonProperty("session")]
        public string Session { get; set; }

        [JsonProperty("immediatelyGoToNext")]
        public bool ImmediatelyGoToNext { get; set;  }

        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("quickReplies")]
        public IEnumerable<QuickReply> QuickReplies { get; set; }
    }
}