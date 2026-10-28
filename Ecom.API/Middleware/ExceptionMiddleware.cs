
using Ecom.API.Helper;
using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace Ecom.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _memoryCache;
        private readonly TimeSpan _rateLimitWindow = TimeSpan.FromSeconds(30);
        private const int RequestLimit = 80;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(
            RequestDelegate next,
            IHostEnvironment environment,
            IMemoryCache memoryCache,
            ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                if (!IsRequestAllowed(context))
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode =
                        (int)HttpStatusCode.TooManyRequests;

                    var response = new ApiExceptions(
                        (int)HttpStatusCode.TooManyRequests,
                        "Too many requests. Please try again later.");

                    await context.Response.WriteAsJsonAsync(response);
                    return;
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred.");

                if (context.Response.HasStarted)
                {
                    throw;
                }

                context.Response.Clear();
                context.Response.ContentType = "application/json";
                context.Response.StatusCode =
                    (int)HttpStatusCode.InternalServerError;

                var response = new ApiExceptions(
                    context.Response.StatusCode,
                    ex.Message,
                    ex.ToString());

                await context.Response.WriteAsJsonAsync(response);
            }
        }

        private bool IsRequestAllowed(HttpContext context)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString()
                     ?? "unknown";

            var cacheKey = $"Rate:{ip}";
            var now = DateTime.UtcNow;

            var (windowStart, count) = _memoryCache.GetOrCreate(
                cacheKey,
                entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow =
                        _rateLimitWindow;

                    return (WindowStart: now, Count: 0);
                });

            if (now - windowStart >= _rateLimitWindow)
            {
                _memoryCache.Set(
                    cacheKey,
                    (WindowStart: now, Count: 1),
                    _rateLimitWindow);

                return true;
            }

            if (count >= RequestLimit)
            {
                return false;
            }

            _memoryCache.Set(
                cacheKey,
                (WindowStart: windowStart, Count: count + 1),
                _rateLimitWindow);

            return true;
        }
    }
}
