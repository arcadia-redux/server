using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Server
{
    public static class ExceptionHandlingExtensions
    {
        public static IApplicationBuilder UseJsonExceptionHandler(this IApplicationBuilder app) =>
            app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
            {
                var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                var exception = exceptionHandlerPathFeature.Error;

                context.Response.StatusCode = 500;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync($"{exception.GetType().Name}: {exception.Message}");
            }));

        // https://github.com/dotnet/AspNetCore.Docs/issues/12157
        public static IMvcBuilder AddInvalidModelStateLogging(this IMvcBuilder builder, Func<HttpContext, bool> predicate)
        {
            builder.Services.PostConfigure<ApiBehaviorOptions>(options =>
            {
                var builtInFactory = options.InvalidModelStateResponseFactory;
                options.InvalidModelStateResponseFactory = context =>
                {
                    if (predicate(context.HttpContext))
                    {
                        var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                        var logger = loggerFactory.CreateLogger(context.ActionDescriptor.DisplayName);
                        var validationErrors = string.Join("\n", context.ModelState
                            .SelectMany(state => state.Value.Errors.Select(error => new { state.Key, error.ErrorMessage }))
                            .Select(e => $"{e.Key}: ${e.ErrorMessage}"));

                        logger.LogError($"One or more validation errors occurred.\n{validationErrors}");
                    }

                    return builtInFactory(context);
                };
            });

            return builder;
        }
    }
}
