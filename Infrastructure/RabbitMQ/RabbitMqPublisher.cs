using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OrderFlow.Core.Configuration;
using OrderFlow.Core.Models;
using RabbitMQ.Client;

namespace OrderFlow.Core.Infrastructure.RabbitMQ;

/// <summary>
/// Implements message publishing to RabbitMQ for order events.
/// </summary>
/// <remarks>
/// This class handles the publishing of order events to RabbitMQ exchange with the following features:
/// - Persistent message delivery with durable exchange
/// - JSON serialization of events
/// - Automatic channel recovery on connection failures
/// - Proper resource cleanup through IDisposable
/// - Comprehensive logging of all publishing operations
/// Messages are published with persistent delivery mode to ensure they survive broker restarts.
/// </remarks>
public class RabbitMqPublisher : IMessagePublisher, IDisposable
{
    private readonly IRabbitMqConnectionFactory _connectionFactory; // Factory for creating RabbitMQ connections
    private readonly RabbitMqSettings _settings; // RabbitMQ configuration settings
    private readonly ILogger<RabbitMqPublisher> _logger;
    private IConnection? _connection; // RabbitMQ connection
    private IModel? _channel; // RabbitMQ channel for publishing messages. A chanel is a virtual connection inside a connection. It is used to perform most of the operations (publish/consume messages, declare queues/exchanges, etc.)

    public RabbitMqPublisher(
        IRabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqPublisher> logger)
    {
        _connectionFactory = connectionFactory;
        _settings = settings.Value;
        _logger = logger;
        InitializeRabbitMq();
    }

    /// <summary>
    /// Initializes the RabbitMQ connection and channel, and declares the exchange for publishing messages.
    /// </summary>
    /// <remarks>This method must be called before publishing messages to RabbitMQ. If initialization fails,
    /// an exception is logged and rethrown. The exchange is declared as durable and not auto-deleted, ensuring it
    /// persists across broker restarts.</remarks>
    private void InitializeRabbitMq()
    {
        try
        {
            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange for publishing. Exchanges must be declared before publishing.
            _channel.ExchangeDeclare(
                exchange: _settings.ExchangeName, // e.g., "order_events_exchange".
                type: _settings.ExchangeType,
                durable: true,
                autoDelete: false);

            _logger.LogInformation($"RabbitMQ Publisher initialized with exchange: {_settings.ExchangeName} and type {_settings.ExchangeType}", 
                _settings.ExchangeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ Publisher");
            throw;
        }
    }

    /// <summary>
    /// Publishes the specified order event to the message broker using the given routing key.
    /// </summary>
    /// <remarks>If the channel to the message broker is closed, it will be reinitialized before publishing.
    /// The message is serialized as JSON and marked as persistent to ensure delivery. This method logs publishing
    /// activity and errors for monitoring purposes.</remarks>
    /// <param name="orderEvent">The order event to be published. Cannot be null.</param>
    /// <param name="routingKey">The routing key used to route the message within the exchange. Cannot be null or empty.</param>
    /// <returns>A task that represents the asynchronous publish operation.</returns>
    public Task PublishAsync(OrderEvent orderEvent, string routingKey)
    {
        try
        {
            if (_channel == null || _channel.IsClosed)
            {
                _logger.LogWarning("Channel is closed, reinitializing...");
                InitializeRabbitMq();
            }

            // Serialize the order event to JSON
            var message = JsonSerializer.Serialize(orderEvent);
            // Convert the message to a byte array
            var body = Encoding.UTF8.GetBytes(message);

            // Create basic properties for the message
            var properties = _channel!.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            // Publish the message to the exchange with the specified routing key
            _channel.BasicPublish(
                exchange: _settings.ExchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation(
                $"Published message to exchange {_settings.ExchangeName} with routing key {routingKey}. OrderId: {orderEvent.OrderId}, EventType: {orderEvent.EventType}");

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message with routing key {RoutingKey}", routingKey);
            throw;
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}
