using System;
using System.Threading.Tasks;

namespace MotionTelegramConnector
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

        public static async Task<U> Retry<U>(this Func<Task<U>> action)
        {
            int i = 0;
            U result;
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