using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OrderFlow.Core.Configuration;
using OrderFlow.Core.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderFlow.Core.Infrastructure.RabbitMQ;

/// <summary>
/// Abstract base class for RabbitMQ message subscribers that run as background services. Implement BackgroundService for running as hosted services.
/// </summary>
/// <remarks>
/// This class provides common functionality for all order event subscribers including:
/// - Queue declaration and binding to exchange with routing keys
/// - Message consumption with manual acknowledgment
/// - Automatic deserialization of JSON messages to OrderEvent objects
/// - Error handling with message requeuing on failures
/// - Connection and channel lifecycle management
/// Derived classes must implement queue name, routing key, and message processing logic.
/// The subscriber uses QoS settings to process one message at a time, ensuring proper order processing.
/// </remarks>
public abstract class RabbitMqSubscriberBase : BackgroundService
{
    protected readonly IRabbitMqConnectionFactory ConnectionFactory;
    protected readonly RabbitMqSettings Settings;
    protected readonly ILogger Logger;
    private IConnection? _connection;
    private IModel? _channel;
    private string? _queueName;

    /// <summary>
    /// Gets the name of the queue this subscriber listens to.
    /// </summary>
    protected abstract string QueueName { get; }
    
    /// <summary>
    /// Gets the routing key pattern used to bind the queue to the exchange.
    /// </summary>
    protected abstract string RoutingKey { get; }

    protected RabbitMqSubscriberBase(
        IRabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqSettings> settings,
        ILogger logger)
    {
        ConnectionFactory = connectionFactory;
        Settings = settings.Value;
        Logger = logger;
    }

    /// <summary>
    /// Executes the background message subscriber loop, initializing the connection, queue, and consumer to process
    /// incoming order events until cancellation is requested.
    /// </summary>
    /// <remarks>This method sets up the messaging infrastructure and continuously listens for incoming
    /// messages on the configured queue. Message processing will stop when the provided cancellation token is
    /// triggered. Exceptions during message handling are logged, and failed messages are requeued for later
    /// processing.</remarks>
    /// <param name="stoppingToken">A cancellation token that can be used to signal the subscriber to stop processing messages and shut down
    /// gracefully.</param>
    /// <returns>A task that represents the asynchronous execution of the subscriber loop. The task completes when cancellation
    /// is requested via the stopping token.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _connection = ConnectionFactory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange
            _channel.ExchangeDeclare(
                exchange: Settings.ExchangeName,
                type: Settings.ExchangeType,
                durable: true,
                autoDelete: false);

            // Declare queue
            _queueName = _channel.QueueDeclare(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false).QueueName;

            // Bind queue to exchange with routing key
            _channel.QueueBind(
                queue: _queueName,
                exchange: Settings.ExchangeName,
                routingKey: RoutingKey);

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            // Create consumer
            var consumer = new EventingBasicConsumer(_channel);

            // Event handler for received messages. Processes messages and acknowledges them.
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var orderEvent = JsonSerializer.Deserialize<OrderEvent>(message);

                    if (orderEvent != null)
                    {
                        Logger.LogInformation(
                            "Received message in {QueueName}. OrderId: {OrderId}, EventType: {EventType}",
                            QueueName, orderEvent.OrderId, orderEvent.EventType);

                        await ProcessMessageAsync(orderEvent);

                        _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        Logger.LogInformation("Successfully processed message in {QueueName}", QueueName);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error processing message in {QueueName}", QueueName);
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

            Logger.LogInformation(
                "Subscriber started for queue {QueueName} with routing key {RoutingKey}",
                QueueName, RoutingKey);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in subscriber {QueueName}", QueueName);
        }
    }

    /// <summary>
    /// Processes the specified order event asynchronously. Is a abstract method that must be implemented by derived classes to define
    /// </summary>
    /// <param name="orderEvent">The order event to be processed. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected abstract Task ProcessMessageAsync(OrderEvent orderEvent);

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }
}
