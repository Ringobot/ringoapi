// https://www.recaffeinate.co/post/serialize-errors-as-json-in-aspnetcore/
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ringo.Api
{
    public class JsonExceptionMiddleware
    {
        private readonly bool _isDevelopment;

        public JsonExceptionMiddleware(bool isDevelopment)
        {
            _isDevelopment = isDevelopment;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
            if (exception == null) return;

            context.Response.ContentType = "application/json";

            try
            {
                var error = new JsonExceptionResponse(exception, _isDevelopment);
                await JsonSerializer.SerializeAsync(
                    context.Response.Body,
                    error, new JsonSerializerOptions { IgnoreNullValues = true });
            }
            catch (Exception ex)
            {
                // Log and continue
                Trace.TraceError(ex.Message);
            }
        }
    }

    public class JsonExceptionResponse
    {
        public JsonExceptionResponse(Exception exception, bool isDevelopment)
        {
            Message = exception.Message;

            if (isDevelopment)
            {
                StackTrace = exception.StackTrace;
                InnerException = exception.InnerException?.Message;
                Source = exception.Source;
            }
        }

        public string Message { get; set; }

        public string Source { get; set; }

        public string InnerException { get; set; }

        public string StackTrace { get; set; }
    }
}
