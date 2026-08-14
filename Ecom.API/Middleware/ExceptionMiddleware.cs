using Ecom.API.Helper;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Text.Json;

namespace Ecom.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHostEnvironment _environment;
        private readonly IMemoryCache _memoryCache;
        private readonly TimeSpan _rateLimitWindow = TimeSpan.FromSeconds(30);
        private readonly ILogger<ExceptionMiddleware> _logger;
        public ExceptionMiddleware(RequestDelegate next, IHostEnvironment environment, IMemoryCache memoryCache, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _environment = environment;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                ApplySecurity(context);

                if (!IsRequestAllowed(context))
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;

                    var Response =
                        new ApiExceptions((int)HttpStatusCode.TooManyRequests, "Too many request. please try again later");

                    await context.Response.WriteAsJsonAsync(Response);
                    return;
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                context.Response.ContentType = "application/json";

                context.Response.StatusCode =
                    (int)HttpStatusCode.InternalServerError;

                var response = new ApiExceptions(
                    context.Response.StatusCode,
                    ex.Message,
                    ex.ToString()
                    );

                var json = JsonSerializer.Serialize(response);

                await context.Response.WriteAsync(json);
            }
        }

        private bool IsRequestAllowed(HttpContext context)
        {
            var ip = context.Connection.RemoteIpAddress.ToString();
            var cachKey = $"Rate:{ip}";
            var dateNow = DateTime.Now;

            var (timesTamp, count) = _memoryCache.GetOrCreate(cachKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _rateLimitWindow;
                return (timesTamp: dateNow, count: 0);
            });

            if (dateNow - timesTamp < _rateLimitWindow)
            {
                if (count >= 80)
                {
                    return false;
                }
                _memoryCache.Set(cachKey, (timesTamp, count += 1), _rateLimitWindow);
            }
            else
            {
                _memoryCache.Set(cachKey, (dateNow, count), _rateLimitWindow);
            }
            return true;
        }
        private void ApplySecurity(HttpContext context)
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-XSS-Protection"] = "1;mode=block";
            context.Response.Headers["X-Frame-Options"] = "DENY";

        }


    }
}