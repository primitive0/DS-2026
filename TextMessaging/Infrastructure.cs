using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace TextMessaging;

public static class Infrastructure
{
    public const string RankQueue = "rank-tasks";
    public static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    public static IServiceCollection AddTextInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton(new ConnectionFactory
        {
            HostName = config["RabbitMQ:HostName"] ?? "localhost",
            UserName = config["RabbitMQ:UserName"] ?? "student",
            Password = config["RabbitMQ:Password"] ?? "student",
            // Sessions are recreated after failure, including initial connection failure.
            AutomaticRecoveryEnabled = false,
            RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
            RequestedHeartbeat = TimeSpan.FromSeconds(10),
            ConsumerDispatchConcurrency = 1
        });
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(config.GetConnectionString("Redis") ?? "localhost:6379");
            options.AbortOnConnectFail = false;
            options.ConnectRetry = 2;
            return ConnectionMultiplexer.Connect(options);
        });
        services.AddSingleton<RabbitPublisher>();
        return services;
    }

    public static Task DeclareRankQueueAsync(IChannel channel, CancellationToken token) =>
        channel.QueueDeclareAsync(RankQueue, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: token);
}
