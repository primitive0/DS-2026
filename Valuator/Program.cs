using StackExchange.Redis;
using Microsoft.AspNetCore.DataProtection;

namespace Valuator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();

        builder.Services
            .AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo("/keys"))
            .SetApplicationName("ValuatorApp");

        builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var cs = builder.Configuration.GetConnectionString("Redis");
            if (string.IsNullOrWhiteSpace(cs))
            {
                throw new InvalidOperationException(
                    "Connection string 'Redis' is not configured.");
            }
            return ConnectionMultiplexer.Connect(cs);
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
    }
}
