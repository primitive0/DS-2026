using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using EventsLogger;
using TextMessaging;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddRabbitMq(builder.Configuration);
builder.Services.AddHostedService<EventsWorker>();
await builder.Build().RunAsync();
