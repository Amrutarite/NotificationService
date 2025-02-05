using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NotificationService.ErrorHandling;
using System;
using System.Net;
using System.Threading.Tasks;

namespace NotificationService.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var errorCode = ErrorCodes.InternalServerError;
            var errorMessage = ErrorMessages.InternalServerErrorMessage;
            var statusCode = (int)HttpStatusCode.InternalServerError;

            // Customize based on exception type
            if (exception is KeyNotFoundException)
            {
                errorCode = ErrorCodes.NotFoundError;
                errorMessage = ErrorMessages.NotFoundErrorMessage;
                statusCode = (int)HttpStatusCode.NotFound;
            }
            else if (exception is InvalidOperationException)
            {
                errorCode = ErrorCodes.ValidationError;
                errorMessage = ErrorMessages.ValidationErrorMessage;
                statusCode = (int)HttpStatusCode.BadRequest;
            }

            var response = new ErrorResponse
            {
                Code = errorCode,
                Message = errorMessage,
                Details = _env.IsDevelopment() ? exception.ToString() : null,
                TraceId = context.TraceIdentifier
            };

            _logger.LogError($"Error Code: {errorCode}, Message: {errorMessage}, TraceId: {context.TraceIdentifier}");

            context.Response.StatusCode = statusCode;
            return context.Response.WriteAsync(JsonConvert.SerializeObject(response));
        }
    }
}
