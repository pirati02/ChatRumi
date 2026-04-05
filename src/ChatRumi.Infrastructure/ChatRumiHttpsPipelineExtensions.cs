using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace ChatRumi.Infrastructure;

public static class ChatRumiHttpsPipelineExtensions
{
    /// <summary>
    /// Enables HSTS and HTTPS redirection in Production only (keeps Integration tests and non-TLS environments on HTTP).
    /// </summary>
    public static WebApplication UseChatRumiHttpsRedirectionAndHsts(this WebApplication app)
    {
        if (!app.Environment.IsProduction())
        {
            return app;
        }

        app.UseHsts();
        app.UseHttpsRedirection();
        return app;
    }
}
