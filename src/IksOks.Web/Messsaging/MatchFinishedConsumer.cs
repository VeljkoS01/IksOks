using System.Text.Json;
using IksOks.Web.Infrastructure.Persistence;
using IksOks.Web.Infrastructure.Persistence.Entities;
using IksOks.Web.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace IksOks.Web.Messaging;

public sealed class MatchFinishedConsumer
    : BackgroundService
{
    private const string RoutingKey =
        "match.finished";

    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MatchFinishedConsumer> _logger;

    private IConnection? _connection;
    private IModel? _channel;

    public MatchFinishedConsumer(
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<MatchFinishedConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            DispatchConsumersAsync = true
        };

        _connection =
            factory.CreateConnection();

        _channel =
            _connection.CreateModel();

        _channel.ExchangeDeclare(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        _channel.QueueDeclare(
            queue: _options.MatchFinishedQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);

        _channel.QueueBind(
            queue: _options.MatchFinishedQueue,
            exchange: _options.ExchangeName,
            routingKey: RoutingKey);

        _channel.BasicQos(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false);

        var consumer =
            new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (_, eventArgs) =>
        {
            await HandleMessageAsync(
                eventArgs,
                stoppingToken);
        };

        _channel.BasicConsume(
            queue: _options.MatchFinishedQueue,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation(
            "RabbitMQ consumer is listening on {Queue}.",
            _options.MatchFinishedQueue);

        return Task.CompletedTask;
    }

    private async Task HandleMessageAsync(
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellationToken)
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            var message =
                JsonSerializer.Deserialize<
                    MatchFinishedEvent>(
                    eventArgs.Body.ToArray());

            if (message is null)
            {
                _channel.BasicNack(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: false);

                return;
            }

            using var scope =
                _scopeFactory.CreateScope();

            var db =
                scope.ServiceProvider
                    .GetRequiredService<
                        IksOksDbContext>();

            var alreadyProcessed =
                await db.MatchFinishedEvents
                    .AnyAsync(
                        item =>
                            item.EventId ==
                            message.EventId,
                        cancellationToken);

            if (!alreadyProcessed)
            {
                db.MatchFinishedEvents.Add(
                    new MatchFinishedEventRecord
                    {
                        EventId =
                            message.EventId,

                        MatchId =
                            message.MatchId,

                        WinnerUserId =
                            message.WinnerUserId,

                        IsDraw =
                            message.IsDraw,

                        FinishedAt =
                            message.FinishedAt
                    });

                await db.SaveChangesAsync(
                    cancellationToken);
            }

            _channel.BasicAck(
                eventArgs.DeliveryTag,
                multiple: false);
        }
        catch (JsonException exception)
        {
            _logger.LogError(
                exception,
                "Invalid RabbitMQ match event.");

            _channel.BasicNack(
                eventArgs.DeliveryTag,
                multiple: false,
                requeue: false);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Could not process match finished event.");

            _channel.BasicNack(
                eventArgs.DeliveryTag,
                multiple: false,
                requeue: true);
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();

        base.Dispose();
    }
}