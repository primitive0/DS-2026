using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
using TextMessaging;

namespace RankCalculator;

public sealed class RankWorker(ConnectionFactory factory, IConnectionMultiplexer redis, RabbitPublisher publisher,
    IConfiguration config, ILogger<RankWorker> logger) : RabbitConsumerService(factory, logger)
{
    protected override async Task<string> DeclareQueueAsync(IChannel channel, CancellationToken token)
    {
        await Infrastructure.DeclareRankQueueAsync(channel, token);
        return Infrastructure.RankQueue;
    }

    protected override async Task HandleAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken token)
    {
        CalculateRank? task;
        try
        {
            task = JsonSerializer.Deserialize<CalculateRank>(delivery.Body.Span);
            if (delivery.BasicProperties.Type != nameof(CalculateRank) || task is null || task.TextId == Guid.Empty)
                throw new JsonException("Expected CalculateRank with a non-empty TextId");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Rejected invalid rank task on {Instance}", Environment.MachineName);
            await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false, token);
            return;
        }

        logger.LogInformation("Processing TextId={TextId} Instance={Instance} Redelivered={Redelivered}",
            task.TextId, Environment.MachineName, delivery.Redelivered);
        var delay = config.GetValue<int>("ProcessingDelayMilliseconds");
        if (delay > 0) await Task.Delay(delay, token);

        while (!token.IsCancellationRequested)
        {
            try
            {
                var db = redis.GetDatabase();
                var text = await db.StringGetAsync(StorageKeys.Text(task.TextId));
                if (text.IsNull)
                {
                    logger.LogError("Rejected task for missing text {TextId}", task.TextId);
                    await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false, token);
                    return;
                }
                var rank = TextRank.Calculate(text.ToString());
                await db.StringSetAsync(StorageKeys.Rank(task.TextId), rank.ToString(CultureInfo.InvariantCulture));
                await publisher.PublishEventAsync(new RankCalculated(task.TextId, rank), token);
                await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, token);
                logger.LogInformation("Rank calculated TextId={TextId} Rank={Rank} Instance={Instance}",
                    task.TextId, rank, Environment.MachineName);
                return;
            }
            catch (RedisException ex)
            {
                logger.LogWarning(ex, "Storage unavailable for {TextId}; retrying in 2 seconds", task.TextId);
                await Task.Delay(Infrastructure.RetryDelay, token);
            }
        }
    }
}
