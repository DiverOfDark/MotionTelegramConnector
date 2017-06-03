namespace MotionTelegramConnector.Services
{
    public class GoogleAnalyticsService
    {
//Для Google Analytics собираем два вида метрик – pageview для вызовов блока диалога и event для полезных действий.
        public static async void LogPageView()
        {
/*
Pageview request:
 
Триггер – вызов любого модуля motion.ai
 
POST request to https://www.google-analytics.com/collect
 
Атрибуты:
 
v=1
tid=UA-98473469-1
t=pageview
dp=rbot\<pagename>  где <pagename> это имя модуля motion.ai (moduleNickname)
cid=<session>  сессия из motion.ai
uid=<user_id> пользователь из Telegram
*/            
        }

        public static async void LogEvent()
        {
            /*
    Event request:
     
    Триггер – вызов одного из модулей: 
     
    positions/apply/get-email
    positions/apply/confirmation
    interview/start
    interview/answer-correct
    ЗадачаXX 
     
     
     
    POST request to https://www.google-analytics.com/collect
     
    Атрибуты:
     
    v=1
    tid=UA-98473469-1
    е=event
    ea=<event_name> где event_name код события
    ec=user_actions
    cid=<session>  из motion.ai
    uid=<user_id> из Telegram
     
     
    Dialog Modules – EventNames
     
    positions/apply/get-email   -  apply_to_position
    positions/apply/confirmation  -  send_application
    interview/start -  interview_start
    interview/answer-correct   -   interview_task_correct
    ЗадачаXX   -  interview_next_task
     
            */        }
    }
}