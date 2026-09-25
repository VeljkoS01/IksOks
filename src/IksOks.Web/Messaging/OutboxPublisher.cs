using System.Text.Json;
using IksOks.Web.Infrastructure.Persistence;
using IksOks.Web.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;

namespace IksOks.Web.Messaging;

public sealed class OutboxPublisher
    : BackgroundService
{
    private const string MatchFinishedRoutingKey =
        "match.finished";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<OutboxPublisher> _logger;

    public OutboxPublisher(
        IServiceScopeFactory scopeFactory,
        IEventPublisher eventPublisher,
        ILogger<OutboxPublisher> logger)
    {
        _scopeFactory = scopeFactory;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingMessagesAsync(
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Could not process outbox messages.");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(2),
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task PublishPendingMessagesAsync(
        CancellationToken cancellationToken)
    {
        using var scope =
            _scopeFactory.CreateScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<IksOksDbContext>();

        var messages =
            await db.OutboxMessages
                .Where(message =>
                    message.ProcessedAt == null)
                .OrderBy(message =>
                    message.OccurredAt)
                .Take(20)
                .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            message.AttemptCount++;

            try
            {
                if (message.RoutingKey !=
                    MatchFinishedRoutingKey)
                {
                    throw new InvalidOperationException(
                        $"Unknown routing key: {message.RoutingKey}");
                }

                var matchFinishedEvent =
                    JsonSerializer.Deserialize<
                        MatchFinishedEvent>(
                        message.Payload);

                if (matchFinishedEvent is null)
                {
                    throw new InvalidOperationException(
                        "Outbox payload could not be deserialized.");
                }

                await _eventPublisher
                    .PublishMatchFinishedAsync(
                        matchFinishedEvent,
                        cancellationToken);

                message.ProcessedAt =
                    DateTimeOffset.UtcNow;

                message.LastError = null;

                _logger.LogInformation(
                    "Outbox message {MessageId} was published.",
                    message.Id);
            }
            catch (Exception exception)
            {
                var error = exception.Message;

                message.LastError =
                    error.Length <= 2000
                        ? error
                        : error[..2000];

                _logger.LogError(
                    exception,
                    "Could not publish outbox message {MessageId}.",
                    message.Id);
            }

            await db.SaveChangesAsync(
                cancellationToken);
        }
    }
}