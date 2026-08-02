using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SMS.Application.Exceptions;

namespace SMS.API.Filters
{
    public class ApiExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<ApiExceptionFilter> _logger;

        public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, "An unhandled exception occurred");

            var response = new
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "An error occurred while processing your request.",
                Timestamp = DateTime.UtcNow
            };

            if (context.Exception is ValidationException validationException)
            {
                context.Result = new BadRequestObjectResult(new
                {
                    StatusCode = 400,
                    Message = "Validation failed",
                    Errors = validationException.Errors,
                    Timestamp = DateTime.UtcNow
                });
                return;
            }

            if (context.Exception is NotFoundException)
            {
                context.Result = new NotFoundObjectResult(new
                {
                    StatusCode = 404,
                    Message = context.Exception.Message,
                    Timestamp = DateTime.UtcNow
                });
                return;
            }

            if (context.Exception is UnauthorizedException)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    StatusCode = 401,
                    Message = context.Exception.Message,
                    Timestamp = DateTime.UtcNow
                });
                return;
            }

            if (context.Exception is ForbiddenException)
            {
                context.Result = new ObjectResult(new
                {
                    StatusCode = 403,
                    Message = context.Exception.Message,
                    Timestamp = DateTime.UtcNow
                })
                { StatusCode = 403 };
                return;
            }

            context.Result = new ObjectResult(response)
            {
                StatusCode = (int)HttpStatusCode.InternalServerError
            };
        }
    }
}