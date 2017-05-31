using System;

namespace MotionTelegramConnector
{
    public class AppSettings
    {
        public string TELEGRAM_API_KEY { get; set; }
        public string WEBSITE_URL { get; set; }
        public string MOTION_API_KEY { get; set; }
        public string MOTION_BOT_ID { get; set; }

        public void Validate()
        {
            if (string.IsNullOrEmpty(TELEGRAM_API_KEY) ||
                string.IsNullOrEmpty(MOTION_API_KEY) ||
                string.IsNullOrEmpty(MOTION_BOT_ID))
            {
                throw new ArgumentNullException("Error on appsettings!");
            }
        }
    }
}