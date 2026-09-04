using System.Text.Json;
using IksOks.Web.Messaging.Contracts;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace IksOks.Web.Messaging;

public sealed class RabbitMqEventPublisher
    : IEventPublisher, IDisposable
{
    private const string MatchFinishedRoutingKey =
        "match.finished";

    private readonly RabbitMqOptions _options;
    private readonly object _connectionLock = new();

    private IConnection? _connection;

    public RabbitMqEventPublisher(
        IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public Task PublishMatchFinishedAsync(
        MatchFinishedEvent message,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var connection = GetConnection();

        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        var body =
            JsonSerializer.SerializeToUtf8Bytes(message);

        var properties =
            channel.CreateBasicProperties();

        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.Type = MatchFinishedRoutingKey;
        properties.MessageId =
            message.EventId.ToString();

        channel.BasicPublish(
            exchange: _options.ExchangeName,
            routingKey: MatchFinishedRoutingKey,
            basicProperties: properties,
            body: body);

        return Task.CompletedTask;
    }

    private IConnection GetConnection()
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        lock (_connectionLock)
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            _connection?.Dispose();

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password
            };

            _connection =
                factory.CreateConnection();

            return _connection;
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}