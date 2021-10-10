using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace WebUpLoadingTest.Infrastructure.Filters
{
    public class GenerateAntiforgeryTokenCookieAttribute : ResultFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            var http_context = context.HttpContext;
            var antiforgery = http_context.RequestServices.GetRequiredService<IAntiforgery>();

            // Send the request token as a JavaScript-readable cookie
            var tokens = antiforgery.GetAndStoreTokens(http_context);

            http_context.Response.Cookies.Append(
                "RequestVerificationToken",
                tokens.RequestToken!, 
                new CookieOptions { HttpOnly = false });
        }

        public override void OnResultExecuted(ResultExecutedContext context)
        {
        }
    }
}
