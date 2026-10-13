using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using RankCalculator;
using TextMessaging;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTextInfrastructure(builder.Configuration);
builder.Services.AddHostedService<RankWorker>();
await builder.Build().RunAsync();
