using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;

namespace MotionTelegramConnector.Services
{
    public class GoogleAnalyticsService
    {
        private class PageView
        {
            public string ModuleName { get; set; }
            public string Session { get; set; }

            public virtual bool IsValid() => !string.IsNullOrWhiteSpace(Session) && !string.IsNullOrWhiteSpace(ModuleName);
        }

        private const string Url = "https://www.google-analytics.com/collect";
        
        private static readonly HttpClient HttpClient = new HttpClient();

        private readonly ConcurrentDictionary<string,string> _sessions = new ConcurrentDictionary<string, string>();
        private readonly string _counter;
        private readonly string _botname;
        
        private readonly ConcurrentQueue<PageView> _pageViews = new ConcurrentQueue<PageView>(); 
        private readonly ConcurrentQueue<PageView> _events = new ConcurrentQueue<PageView>();
        private Timer _timer;

        public GoogleAnalyticsService(AppSettings settings)
        {
            _counter = settings.GA_COUNTER;
            _botname = settings.GA_BOTNAME;

            new Thread(StartBackgroundThread) {IsBackground = true}.Start();
        }

        private async void StartBackgroundThread()
        {
            while (true)
            {
                var pageViews = new List<PageView>();
                var pvEvents = new List<PageView>();

                PageView item = null;
                while (_pageViews.TryDequeue(out item))
                {
                    if (_sessions.ContainsKey(item.Session))
                    {
                        var user = _sessions[item.Session];
                        var keys = new Dictionary<string, string>
                        {
                            {"v", "1"},
                            {"tid", _counter},
                            {"t", "pageview"},
                            {"dp", _botname + "\\" + item.ModuleName},
                            {"cid", item.Session},
                            {"uid", user}
                        };
                        var content = new FormUrlEncodedContent(keys);
                        await HttpClient.PostAsync(Url, content);
                    }
                    else
                    {
                        pageViews.Add(item);
                    }
                }
                while (_events.TryDequeue(out item))
                {
                    if (_sessions.ContainsKey(item.Session))
                    {
                        var user = _sessions[item.Session];
                        var keys = new Dictionary<string, string>
                        {
                            {"v", "1"},
                            {"tid", _counter},
                            {"t", "event"},
                            {"ea", item.ModuleName},
                            {"ec", "user_actions"},
                            {"cid", item.Session},
                            {"uid", user}
                        };
                        var content = new FormUrlEncodedContent(keys);
                        await HttpClient.PostAsync(Url, content);
                    }
                    else
                    {
                        pvEvents.Add(item);
                    }
                }

                foreach (var baditem in pageViews)
                {
                    _pageViews.Enqueue(baditem);
                }

                foreach (var baditem in pvEvents)
                {
                    _events.Enqueue(baditem);
                }
                
                Thread.Sleep(1000);
            }
        }

        public void LogPageView(string moduleName, string session)
        {
            var pageView = new PageView
            {
                Session = session,
                ModuleName = moduleName
            };

            if (pageView.IsValid())
            {
                _pageViews.Enqueue(pageView);
            }
        }

        public void LogEvent(string eventName, string session)
        {
            var pageView = new PageView
            {
                Session = session,
                ModuleName = eventName
            };

            if (pageView.IsValid())
            {
                _events.Enqueue(pageView);
            }
        }
        
        public void RegisterSessionUser(string session, string user)
        {
            _sessions[session] = user;
            
            LogPageView(null, null);
            LogEvent(null, null);
        }
    }
}