using System;
using System.Threading.Tasks;

namespace MotionTelegramConnector.Services
{
    public static class Extensions
    {
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