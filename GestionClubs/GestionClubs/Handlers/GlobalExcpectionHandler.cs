using GestionClubs.Application.BaseExceptions;
using GestionClubs.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;

namespace GestionClubs.API.Handlers
{
    public class GlobalExcpectionHandler: IExceptionHandler
    {
        
            private readonly ILogger<GlobalExcpectionHandler> _logger;
            public GlobalExcpectionHandler(ILogger<GlobalExcpectionHandler> logger)
            {
                _logger = logger;
            }
            public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
            {
                var (statusCode, message) = GetExceptionDetails(exception);
                _logger.LogError(exception, exception.Message);
                httpContext.Response.StatusCode = (int)statusCode;
                await httpContext.Response.WriteAsJsonAsync(message, cancellationToken);
                return true;
            }
            private static (HttpStatusCode statusCode, string message) GetExceptionDetails(Exception exception)
            {
                return exception switch
                {
                    AppUnauthorizedException => (HttpStatusCode.Forbidden, exception.Message),
                    AppConflictException => (HttpStatusCode.Conflict, exception.Message),
                    BadHttpRequestException => (HttpStatusCode.BadRequest, exception.Message),
                    EntityNotFoundException => (HttpStatusCode.NotFound, exception.Message),
                    InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message),
                    _ => (HttpStatusCode.InternalServerError, $"Unexpected error : {exception.Message}")

                };
            }
        
    }
}
