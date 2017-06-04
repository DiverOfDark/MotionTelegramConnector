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
        
        private readonly Queue<PageView> _pageViews = new Queue<PageView>(); 
        private readonly Queue<PageView> _events = new Queue<PageView>(); 

        public GoogleAnalyticsService(AppSettings settings)
        {
            _counter = settings.GA_COUNTER;
            _botname = settings.GA_BOTNAME;
        }
        
        public async void LogPageView(string moduleName, string session)
        {
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

            var list = _pageViews.ToList();

            while (_pageViews.Count > 0)
            {
                var item = _pageViews.Dequeue();
                if (_sessions.ContainsKey(item.Session))
                {
                    var user = _sessions[item.Session];
                    var keys = new Dictionary<string, string>
                    {
                        {"v","1"},
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
                    list.Add(item);
                }
            }

            foreach (var item in list)
            {
                _pageViews.Enqueue(item);
            }
        }

        public async void LogEvent(string eventName, string session)
        {
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

            var list = _events.ToList();

            while (_events.Count > 0)
            {
                var item = _events.Dequeue();
                if (_sessions.ContainsKey(item.Session))
                {
                    var user = _sessions[item.Session];
                    var keys = new Dictionary<string, string>
                    {
                        {"v", "1"},
                        {"tid", _counter},
                        {"e", "event"},
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
                    list.Add(item);
                }
            }

            foreach (var item in list)
            {
                _events.Enqueue(item);
            }
            
            /*
Dialog Modules – EventNames

positions/apply/get-email   -  apply_to_position
positions/apply/confirmation  -  send_application
interview/start -  interview_start
interview/answer-correct   -   interview_task_correct
ЗадачаXX   -  interview_next_task

*/
        }

        public void RegisterSessionUser(string session, string user)
        {
            _sessions[session] = user;
            
            LogPageView(null, null);
            LogEvent(null, null);
        }
    }
}