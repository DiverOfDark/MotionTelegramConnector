using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;

namespace MotionTelegramConnector
{
    public static class EnableRequestRewindExtension
    {
        public class EnableRequestRewindMiddleware
        {
            private readonly RequestDelegate _next;

            public EnableRequestRewindMiddleware(RequestDelegate next)
            {
                _next = next;
            }

            public async Task Invoke(HttpContext context)
            {
                context.Request.EnableRewind();
                await _next(context);
            }
        }

        public static IApplicationBuilder UseEnableRequestRewind(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<EnableRequestRewindMiddleware>();
        }
    }
}