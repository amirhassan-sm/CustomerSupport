using Application.Framework.OperationResult;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using MvcJsonOptions = Microsoft.AspNetCore.Mvc.JsonOptions;

namespace CustomerSupport.ExceptionHandling
{
    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IHostEnvironment _environment;
        private readonly JsonSerializerOptions _jsonOptions;

        public GlobalExceptionHandler(
            ILogger<GlobalExceptionHandler> logger,
            IHostEnvironment environment,
            IOptions<MvcJsonOptions> jsonOptions)
        {
            _logger = logger;
            _environment = environment;
            _jsonOptions = jsonOptions.Value.JsonSerializerOptions;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            if (exception is OperationCanceledException)
                return false;

            _logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path}.",
                httpContext.Request.Method,
                httpContext.Request.Path);

            var errors = new List<string> { "An unexpected error occurred." };
            if (_environment.IsDevelopment())
                errors.Add(exception.Message);

            var result = OperationResult.ToFail(
                "An unexpected error occurred.",
                errors,
                "EXCEPTION_OCCURRED",
                HttpStatusCode.InternalServerError);

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            httpContext.Response.ContentType = "application/json";

            await httpContext.Response.WriteAsJsonAsync(result, _jsonOptions, cancellationToken);
            return true;
        }
    }
}
