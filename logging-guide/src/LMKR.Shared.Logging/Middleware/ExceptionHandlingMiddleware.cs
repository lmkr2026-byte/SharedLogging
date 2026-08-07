using LMKR.Shared.Logging.Exceptions;
using LMKR.Shared.Logging.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace LMKR.Shared.Logging.Middleware
{
    public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;

        // Main method to handle the request
        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        // Method to handle exceptions
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var problemDetails = new ResponseModel<object?>(
                (int)HttpStatusCode.InternalServerError
            );

            switch (exception)
            {
                case NotFoundException:
                case KeyNotFoundException:
                    problemDetails.StatusCode = (int)HttpStatusCode.NotFound;
                    problemDetails.Title = "Not Found";
                    problemDetails.Message = exception.Message;
                    problemDetails.IsSuccess = false;
                    break;

                case ValidationException ex:
                    problemDetails.StatusCode = (int)HttpStatusCode.BadRequest;
                    problemDetails.Title = "Validation Error";
                    problemDetails.Message = ex.Message;
                    problemDetails.IsSuccess = false;
                    break;

                case BadRequestException:
                    problemDetails.StatusCode = (int)HttpStatusCode.BadRequest;
                    problemDetails.Title = "Bad Request";
                    problemDetails.Message = exception.Message;
                    problemDetails.IsSuccess = false;
                    break;

                case AppException:
                    problemDetails.StatusCode = (int)HttpStatusCode.InternalServerError;
                    problemDetails.Title = "Application Error";
                    problemDetails.Message = exception.Message;
                    problemDetails.IsSuccess = false;
                    break;

                default:
                    problemDetails.StatusCode = (int)HttpStatusCode.InternalServerError;
                    problemDetails.Title = "Internal Server Error";
                    problemDetails.Message = exception.Message;
                    problemDetails.IsSuccess = false;
                    break;
            }


            _logger.LogError(exception, message: string.Concat("An exception occurre: ", exception.Message));
            var result = JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(result);
        }
    }
}
