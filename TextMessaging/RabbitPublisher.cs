using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace TextMessaging;

public sealed class RabbitPublisher(ConnectionFactory factory, ILogger<RabbitPublisher> logger) : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public Task PublishRankTaskAsync(Guid id, CancellationToken token) =>
        PublishAsync("", Infrastructure.RankQueue, new CalculateRank(id), mandatory: true, token);

    public async Task EnsureReadyAsync(CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        await _gate.WaitAsync(timeout.Token);
        try { await EnsureChannelAsync(timeout.Token); }
        catch
        {
            await ResetAsync();
            throw;
        }
        finally { _gate.Release(); }
    }

    private async Task PublishAsync<T>(string exchange, string routingKey, T message, bool mandatory, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        await _gate.WaitAsync(timeout.Token);
        try
        {
            await EnsureChannelAsync(timeout.Token);
            var properties = new BasicProperties
            {
                ContentType = "application/json",
                Type = typeof(T).Name,
                Persistent = true
            };
            // Confirmation tracking makes this await include the broker's acknowledgement.
            await _channel!.BasicPublishAsync(exchange, routingKey, mandatory, properties,
                JsonSerializer.SerializeToUtf8Bytes(message), timeout.Token);
        }
        catch
        {
            await ResetAsync();
            throw;
        }
        finally { _gate.Release(); }
    }

    private async Task EnsureChannelAsync(CancellationToken token)
    {
        if (_connection?.IsOpen == true && _channel?.IsOpen == true) return;
        await ResetAsync();
        _connection = await factory.CreateConnectionAsync($"publisher-{Environment.MachineName}", token);
        _channel = await _connection.CreateChannelAsync(new CreateChannelOptions(
            publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), token);
        await Infrastructure.DeclareRankQueueAsync(_channel, token);
        logger.LogInformation("RabbitMQ publisher connected on {Instance}", Environment.MachineName);
    }

    private async Task ResetAsync()
    {
        var channel = _channel;
        var connection = _connection;
        _channel = null;
        _connection = null;
        try { if (channel is not null) await channel.DisposeAsync(); }
        catch (Exception ex) { logger.LogDebug(ex, "Closing publisher channel"); }
        try { if (connection is not null) await connection.DisposeAsync(); }
        catch (Exception ex) { logger.LogDebug(ex, "Closing publisher connection"); }
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync();
        try { await ResetAsync(); }
        finally { _gate.Release(); }
    }
}
