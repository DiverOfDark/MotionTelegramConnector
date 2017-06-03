using Newtonsoft.Json;

namespace MotionTelegramConnector.Services
{
    [JsonObject]
    public class QuickReply
    {
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("_id")]
        public string Id { get; set; }
        [JsonProperty("payload")]
        public string Payload { get; set; }
        [JsonProperty("content_type")]
        public string ContentType { get; set; }
    }
}