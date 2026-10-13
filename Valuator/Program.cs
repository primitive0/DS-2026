using StackExchange.Redis;
using Microsoft.AspNetCore.DataProtection;
using TextMessaging;

namespace Valuator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddRazorPages();
        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo("/keys"))
            .SetApplicationName("ValuatorApp");
        builder.Services.AddTextInfrastructure(builder.Configuration);

        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Valuator-Instance"] = Environment.MachineName;
            await next();
        });
        if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Error");
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthorization();
        app.MapRazorPages();
        app.MapGet("/health", async (IConnectionMultiplexer redis, RabbitPublisher publisher, CancellationToken token) =>
        {
            try
            {
                await redis.GetDatabase().PingAsync();
                await publisher.EnsureReadyAsync(token);
                return Results.Ok("ready");
            }
            catch { return Results.StatusCode(503); }
        });
        app.Run();
    }
}
