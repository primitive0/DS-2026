using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TextMessaging;

namespace EventsLogger;

public sealed class EventsWorker(ConnectionFactory factory, ILogger<EventsWorker> logger)
    : RabbitConsumerService(factory, logger)
{
    protected override async Task<string> DeclareQueueAsync(IChannel channel, CancellationToken token)
    {
        await Infrastructure.DeclareEventsExchangeAsync(channel, token);
        var queue = await channel.QueueDeclareAsync("", durable: false, exclusive: true, autoDelete: true,
            cancellationToken: token);
        await channel.QueueBindAsync(queue.QueueName, Infrastructure.EventsExchange, "", cancellationToken: token);
        return queue.QueueName;
    }

    protected override async Task HandleAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken token)
    {
        Guid textId;
        double value;
        string metric;
        var eventType = delivery.BasicProperties.Type;
        try
        {
            switch (eventType)
            {
                case nameof(RankCalculated):
                    var rank = Deserialize<RankCalculated>(delivery);
                    textId = rank.TextId;
                    value = rank.Rank;
                    metric = nameof(RankCalculated.Rank);
                    break;
                case nameof(SimilarityCalculated):
                    var similarity = Deserialize<SimilarityCalculated>(delivery);
                    textId = similarity.TextId;
                    value = similarity.Similarity;
                    metric = nameof(SimilarityCalculated.Similarity);
                    break;
                default:
                    throw new JsonException($"Unknown event type: {eventType}");
            }
            if (textId == Guid.Empty || !double.IsFinite(value) || value < 0 || value > 1)
                throw new JsonException("Expected a non-empty TextId and a metric between 0 and 1");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Rejected invalid event Type={Type} Instance={Instance}",
                eventType, Environment.MachineName);
            await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false, token);
            return;
        }

        logger.LogInformation("Event Type={Type} TextId={TextId} {Metric}={Value} Instance={Instance}",
            eventType, textId, metric, value.ToString(CultureInfo.InvariantCulture), Environment.MachineName);
        await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, token);
    }

    private static T Deserialize<T>(BasicDeliverEventArgs delivery) where T : class =>
        JsonSerializer.Deserialize<T>(delivery.Body.Span) ?? throw new JsonException("Event payload is null");
}
