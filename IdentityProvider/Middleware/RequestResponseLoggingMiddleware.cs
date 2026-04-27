using System.Text;

namespace IdentityProvider.Middleware
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestId = Guid.NewGuid().ToString();

            // Log incoming request
            await LogRequestAsync(context, requestId);

            // Store the original response stream
            var originalResponseStream = context.Response.Body;

            using var responseStream = new MemoryStream();
            context.Response.Body = responseStream;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                // Log response
                await LogResponseAsync(context, requestId, stopwatch.ElapsedMilliseconds);

                // Copy response back to original stream
                responseStream.Position = 0;
                await responseStream.CopyToAsync(originalResponseStream);
            }
        }

        private async Task LogRequestAsync(HttpContext context, string requestId)
        {
            var request = context.Request;

            _logger.LogInformation(
                "[{RequestId}] Incoming Request: {Method} {Path} {QueryString} from {RemoteIp}",
                requestId,
                request.Method,
                request.Path,
                request.QueryString,
                context.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
            );

            // Log headers (excluding sensitive ones)
            var headers = request.Headers
                .Where(h => !IsSensitiveHeader(h.Key))
                .ToDictionary(h => h.Key, h => string.Join(", ", h.Value.AsEnumerable()));


            if (headers.Any())
            {
                _logger.LogDebug(
                    "[{RequestId}] Request Headers: {@Headers}",
                    requestId,
                    headers
                );
            }

            // Log request body for POST/PUT requests
            if (request.Method is "POST" or "PUT" && request.HasFormContentType == false)
            {
                request.EnableBuffering();
                var body = await ReadRequestBodyAsync(request);
                if (!string.IsNullOrEmpty(body))
                {
                    _logger.LogDebug(
                        "[{RequestId}] Request Body: {Body}",
                        requestId,
                        body
                    );
                }
                
            }
        }

        private async Task LogResponseAsync(HttpContext context, string requestId, long elapsedMilliseconds)
        {
            var response = context.Response;

            _logger.LogInformation(
                "[{RequestId}] Response: {StatusCode} in {ElapsedMs}ms",
                requestId,
                response.StatusCode,
                elapsedMilliseconds
            );

            // Log response headers
            var headers = response.Headers
                .Where(h => !IsSensitiveHeader(h.Key))
                .ToDictionary(h => h.Key, h => string.Join(", ", h.Value.AsEnumerable()));


            if (headers.Any())
            {
                _logger.LogDebug(
                    "[{RequestId}] Response Headers: {@Headers}",
                    requestId,
                    headers
                );
            }

            // Log response body for non-binary content
            if (ShouldLogResponseBody(response))
            {
                response.Body.Position = 0;
                var body = await new StreamReader(response.Body).ReadToEndAsync();
                if (!string.IsNullOrEmpty(body))
                {
                    _logger.LogDebug(
                        "[{RequestId}] Response Body: {Body}",
                        requestId,
                        body
                    );
                }
                response.Body.Position = 0;
            }
        }

        private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            request.Body.Position = 0; // S'assurer qu'on lit depuis le début
            var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0; // Remettre à zéro pour les middlewares suivants
            return body;
        }

        private static bool IsSensitiveHeader(string headerName)
        {
            var sensitiveHeaders = new[] 
            { 
                "authorization", 
                "cookie", 
                "set-cookie", 
                "x-api-key",
                "x-auth-token"
            };

            return sensitiveHeaders.Contains(headerName.ToLowerInvariant());
        }

        private static bool ShouldLogResponseBody(HttpResponse response)
        {
            var contentType = response.ContentType?.ToLowerInvariant() ?? "";

            return contentType.Contains("application/json") ||
                   contentType.Contains("application/xml") ||
                   contentType.Contains("text/");
        }
    }
}