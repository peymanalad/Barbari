using System.Net;
using BarcopoloWebApi.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BarcopoloWebApi.Infrastructure.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var problem = new ProblemDetails
            {
                Title = "An error occurred while processing your request.",
                Detail = exception.Message
            };

            problem.Status = exception switch
            {
                BadRequestException => (int)HttpStatusCode.BadRequest,
                AppException => (int)HttpStatusCode.BadRequest,
                NotFoundException => (int)HttpStatusCode.NotFound,
                ForbiddenAccessException => (int)HttpStatusCode.Forbidden,
                UnauthorizedAccessAppException => (int)HttpStatusCode.Unauthorized,
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                InsufficientFundsException => (int)HttpStatusCode.PaymentRequired,
                _ => (int)HttpStatusCode.InternalServerError
            };

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = problem.Status.Value;
            return context.Response.WriteAsJsonAsync(problem);
        }
    }
}