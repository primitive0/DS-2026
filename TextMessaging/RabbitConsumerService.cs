using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace TextMessaging;

public abstract class RabbitConsumerService(ConnectionFactory factory, ILogger logger) : BackgroundService
{
    protected abstract Task<string> DeclareQueueAsync(IChannel channel, CancellationToken token);
    protected abstract Task HandleAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken token);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var session = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                await using var connection = await factory.CreateConnectionAsync(
                    $"{GetType().Name}-{Environment.MachineName}", session.Token);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: session.Token);
                connection.ConnectionShutdownAsync += (_, _) => { session.Cancel(); return Task.CompletedTask; };
                channel.ChannelShutdownAsync += (_, _) => { session.Cancel(); return Task.CompletedTask; };
                try
                {
                    var queue = await DeclareQueueAsync(channel, session.Token);
                    await channel.BasicQosAsync(0, 1, false, session.Token);
                    var consumer = new AsyncEventingBasicConsumer(channel);
                    consumer.ReceivedAsync += async (_, delivery) =>
                    {
                        try { await HandleAsync(channel, delivery, session.Token); }
                        catch (OperationCanceledException) when (session.IsCancellationRequested) { }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Consumer session failed; reconnecting");
                            session.Cancel();
                        }
                    };
                    consumer.UnregisteredAsync += (_, _) => { session.Cancel(); return Task.CompletedTask; };
                    await channel.BasicConsumeAsync(queue, autoAck: false, consumer, session.Token);
                    logger.LogInformation("Consumer ready: {Instance}, queue {Queue}", Environment.MachineName, queue);
                    await Task.Delay(Timeout.Infinite, session.Token);
                }
                finally { session.Cancel(); }
            }
            catch (OperationCanceledException)
            {
                if (stoppingToken.IsCancellationRequested) break;
                logger.LogWarning("RabbitMQ session interrupted; reconnecting in 2 seconds");
            }
            catch (Exception ex) { logger.LogWarning(ex, "RabbitMQ session ended; reconnecting in 2 seconds"); }

            try { await Task.Delay(Infrastructure.RetryDelay, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
