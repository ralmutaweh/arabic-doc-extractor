using System.Diagnostics;

namespace ArabicPdfReader.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<RequestLoggingMiddleware> logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            this.next = next;
            this.logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopWatch = Stopwatch.StartNew();

            string method = context.Request.Method;
            string path = context.Request.Path;

            await next(context);

            int statusCode = context.Response.StatusCode;

            stopWatch.Stop();

            // Structured logging
            // This allows logging providers to index variables as a separate searchable field
            logger.LogInformation("{Method} {Path} responded {StatusCode} in {ElapsedMS}ms",
            method, path, statusCode, stopWatch.ElapsedMilliseconds);
        }
    }
}