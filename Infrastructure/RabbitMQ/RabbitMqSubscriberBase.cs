using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OrderFlow.Core.Configuration;
using OrderFlow.Core.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderFlow.Core.Infrastructure.RabbitMQ;

/// <summary>
/// Abstract base class for RabbitMQ message subscribers that run as background services.
/// </summary>
/// <remarks>
/// Provides common functionality for all order event subscribers including:
/// - Queue declaration and binding to exchange with routing keys
/// - Message consumption with manual acknowledgment
/// - Automatic deserialization of JSON messages to OrderEvent objects
/// - Error handling with message requeuing on failures
/// - Connection and channel lifecycle management
/// 
/// Queue names and routing keys are configured in appsettings.json under RabbitMq:Subscribers section.
/// The subscriber uses QoS settings to process one message at a time.
/// </remarks>
public abstract class RabbitMqSubscriberBase : BackgroundService
{
    protected readonly IRabbitMqConnectionFactory _connectionFactory;
    protected readonly RabbitMqSettings _rabbitMqSettings; // Loaded from configuration
    protected readonly ILogger _logger;
    
    private IConnection? _connection;
    private IModel? _channel;
    private string? _queueName;
    private readonly SubscriberConfig _subscriberConfig; // Configuration for this subscriber   

    /// <summary>
    /// Gets the configuration key for this subscriber (e.g., "OrderProcessing", "Notification").
    /// </summary>
    protected abstract string ConfigurationKey { get; }

    protected RabbitMqSubscriberBase(
        IRabbitMqConnectionFactory connectionFactory,
        // Injected RabbitMQ settings from configuration. IOptions<T> is used for accessing configuration settings in a type-safe manner. This pattern is called Options Pattern.
        // What IOptions<T> provides:
        // 1. Strongly typed and thread-safe access to configuration settings.
        // 2. Centralized configuration management.
        // 3. Support for configuration reloading.
        // 4. Integration with dependency injection.
        // 5. Validation support.
        // 6. Separation of concerns and lazy loading of configuration values, which can improve performance.
        // In this case, it allows the subscriber to access RabbitMQ settings defined in appsettings.json.
        IOptions<RabbitMqSettings> settings, 
        ILogger logger)
    {
        _connectionFactory = connectionFactory;
        _rabbitMqSettings = settings.Value;
        _logger = logger;

        // This retrieves the specific subscriber configuration based on the provided key. ( e.g,., "OrderProcessing", "Notification", "PaymentVerification", "Shipping")
        _subscriberConfig = GetSubscriberConfig(ConfigurationKey);
    }


    /// <summary>
    /// Retrieves the subscriber configuration associated with the specified key. Everything comes from appsettings.json and is mapped to RabbitMqSettings.
    /// </summary>
    /// <param name="key">The identifier for the subscriber configuration to retrieve. Supported values include "OrderProcessing",
    /// "Notification", "PaymentVerification", and "Shipping".</param>
    /// <returns>The <see cref="SubscriberConfig"/> instance corresponding to the specified key.</returns>
    /// <exception cref="InvalidOperationException">Thrown if <paramref name="key"/> does not match a known subscriber configuration, or if the resulting
    /// configuration has an unconfigured <c>QueueName</c> or <c>RoutingKey</c>.</exception>
    private SubscriberConfig GetSubscriberConfig(string key)
    {
        var config = key switch
        {
            "OrderProcessing" => _rabbitMqSettings.Subscribers.OrderProcessing,
            "Notification" => _rabbitMqSettings.Subscribers.Notification,
            "PaymentVerification" => _rabbitMqSettings.Subscribers.PaymentVerification,
            "Shipping" => _rabbitMqSettings.Subscribers.Shipping,
            _ => throw new InvalidOperationException($"Unknown subscriber configuration key: {key}")
        };
        // Validate that essential properties are set
        if (string.IsNullOrEmpty(config.QueueName))
            throw new InvalidOperationException($"QueueName is not configured for subscriber: {key}");
            
        if (string.IsNullOrEmpty(config.RoutingKey))
            throw new InvalidOperationException($"RoutingKey is not configured for subscriber: {key}");
        
        return config; // Return a valid SubscriberConfig instance of the specified key. ( e.g { QueueName = "order_processing_queue", RoutingKey = "order.created" } )
    }


    /// <summary>
    /// Overrides the background service execution method to initialize RabbitMQ infrastructure and start. Main orchestration method.
    /// </summary>
    /// <remarks>This method is typically called by the host to start the background service. The subscriber
    /// remains active until the cancellation token is triggered, at which point it performs a graceful shutdown. Any
    /// unhandled exceptions during execution are logged and rethrown.</remarks>
    /// <param name="stoppingToken">A cancellation token that signals when the subscriber should stop processing and shut down gracefully.</param>
    /// <returns>A task that represents the asynchronous execution of the subscriber service. The task completes when
    /// cancellation is requested via the stopping token.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            InitializeInfrastructure(); // Set up connection, channel, exchange, queue, and binding
            SetupConsumer();              // Start consuming messages from the queue

            _logger.LogInformation(
                "[RabbitMqSubscriberBase] ✅ Subscriber started successfully - Queue: {QueueName}, RoutingKey: {RoutingKey}",
                _subscriberConfig.QueueName,
                _subscriberConfig.RoutingKey);

            // Keep the service running until cancellation is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "[RabbitMqSubscriberBase] 🛑 Subscriber stopping gracefully - Queue: {QueueName}",
                _subscriberConfig.QueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[RabbitMqSubscriberBase] 💥 Critical error in subscriber - Queue: {QueueName}",
                _subscriberConfig.QueueName);
            throw;
        }
    }

    /// <summary>
    /// Initializes the RabbitMQ infrastructure required for message subscription, including connection, channel,
    /// exchange, queue, and binding setup. Each method is responsible for a specific aspect of the infrastructure. ( e.g., DeclareExchange, DeclareQueue, BindQueueToExchange, ConfigureQos).
    /// </summary>
    /// <remarks>This method prepares the subscriber to receive messages by establishing the necessary
    /// RabbitMQ resources. It should be called before attempting to consume messages from the queue. Subsequent calls
    /// will reinitialize the infrastructure, which may affect existing connections or subscriptions.</remarks>
    private void InitializeInfrastructure()
    {
        _logger.LogInformation(
            "[RabbitMqSubscriberBase] 🔌 Initializing RabbitMQ subscriber - Queue: {QueueName}, RoutingKey: {RoutingKey}",
            _subscriberConfig.QueueName,
            _subscriberConfig.RoutingKey);

        // 1. Create connection and channel
        _connection = _connectionFactory.CreateConnection();
        // 2. Create channel on the connection. Channels are used to perform messaging operations.
        _channel = _connection.CreateModel();
        // 3. Declare exchange for publishing and subscribing to messages.
        DeclareExchange();
        // 4. Declare queue
        DeclareQueue();
        // 5. Bind queue to exchange with routing key. This ensures messages with the specified routing key are routed to the queue.
        BindQueueToExchange();
        // 6. Configure Quality of Service (QoS) to process one message at a time.
        ConfigureQos();
    }


    /// <summary>
    /// Declares the exchange on the message broker using the configured exchange name and type.
    /// </summary>
    /// <remarks>The exchange is created as durable and will not be automatically deleted. This method should
    /// be called before publishing or consuming messages that rely on the exchange. If the exchange already exists with
    /// the same parameters, this operation is idempotent.</remarks>
    private void DeclareExchange()
    {
        _channel!.ExchangeDeclare(
            exchange: _rabbitMqSettings.ExchangeName,
            type: _rabbitMqSettings.ExchangeType,
            durable: true,
            autoDelete: false);

        _logger.LogDebug(
            "[RabbitMqSubscriberBase] 📡 Exchange declared - Name: {ExchangeName}",
            _rabbitMqSettings.ExchangeName);
    }

    /// <summary>
    /// Declares a durable, non-exclusive, non-auto-deleted queue on the current channel and updates the internal queue
    /// name reference.
    /// </summary>
    /// <remarks>This method ensures that the queue exists before performing operations that require it. The
    /// queue will persist across broker restarts and can be accessed by multiple connections. The method is intended
    /// for internal use and does not return a value.</remarks>
    private void DeclareQueue()
    {
        _queueName = _channel!.QueueDeclare(
            queue: _subscriberConfig.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false).QueueName;

        _logger.LogDebug(
            "[RabbitMqSubscriberBase] 📦 Queue declared - Name: {QueueName}",
            _queueName);
    }

    private void BindQueueToExchange()
    {
        _channel!.QueueBind(
            queue: _queueName,
            exchange: _rabbitMqSettings.ExchangeName,
            routingKey: _subscriberConfig.RoutingKey);

        _logger.LogDebug(
            "[RabbitMqSubscriberBase] 🔗 Queue bound to exchange - RoutingKey: {RoutingKey}",
            _subscriberConfig.RoutingKey);
    }

    private void ConfigureQos()
    {
        _channel!.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        
        _logger.LogDebug(
            "[RabbitMqSubscriberBase] ⚙️ QoS configured - PrefetchCount: 1");
    }

    /// <summary>
    /// Initializes the message consumer and begins listening for incoming messages on the configured queue.
    /// </summary>
    /// <remarks>This method sets up an event-driven consumer for the queue specified by <c>_queueName</c> and
    /// subscribes to message events. Messages are not acknowledged automatically; manual acknowledgment is required in
    /// the message handler. This method should be called once per channel to avoid multiple consumers on the same
    /// queue.</remarks>
    private void SetupConsumer()
    {
        var consumer = new EventingBasicConsumer(_channel); // Create a new consumer instance, EventingBasicConsumer is event-driven class that raises events for message delivery.
        consumer.Received += OnMessageReceived; // Attach event handler for incoming messages
        _channel!.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
        
        _logger.LogDebug(
            "[RabbitMqSubscriberBase] 🎧 Consumer setup complete - Queue: {QueueName}",
            _queueName);
    }

    /// <summary>
    /// Handles an incoming message from the message broker and processes it asynchronously.
    /// </summary>
    /// <remarks>This method decodes, deserializes, and processes messages received from the broker. If the
    /// message cannot be deserialized, it is rejected without requeuing. Errors during processing are handled and
    /// logged appropriately. This method is intended to be used as an event handler for message delivery
    /// events.</remarks>
    /// <param name="sender">The source of the event, typically the message broker or consumer instance.</param>
    /// <param name="ea">The event data containing details about the delivered message, including its body, routing key, and delivery
    /// tag.</param>
    private async void OnMessageReceived(object? sender, BasicDeliverEventArgs ea)
    {
        var deliveryTag = ea.DeliveryTag;
        var routingKey = ea.RoutingKey;

        try
        {
            var message = DecodeMessage(ea.Body, routingKey); // Decode message body from bytes to string
            var orderEvent = DeserializeMessage(message, deliveryTag); // Deserialize JSON to OrderEvent object

            if (orderEvent == null)
            {
                RejectMessage(deliveryTag, requeue: false);
                return;
            }

            await ProcessAndAcknowledgeMessage(orderEvent, deliveryTag); // Process message and send acknowledgment. Acknowledgment confirms successful processing to the broker.
        }
        catch (JsonException jsonEx)
        {
            HandleJsonError(jsonEx, deliveryTag);
        }
        catch (Exception ex)
        {
            HandleProcessingError(ex, deliveryTag, routingKey);
        }
    }

    /// <summary>
    /// Decodes the specified message body from UTF-8 bytes to a string representation.
    /// </summary>
    /// <remarks>The method assumes the message body is encoded in UTF-8. The routing key is used for
    /// diagnostic logging but does not affect decoding.</remarks>
    /// <param name="body">The message body as a read-only memory buffer containing UTF-8 encoded bytes to decode.</param>
    /// <param name="routingKey">The routing key associated with the message, used for logging and message identification.</param>
    /// <returns>A string containing the decoded message content.</returns>
    private string DecodeMessage(ReadOnlyMemory<byte> body, string routingKey)
    {
        var bytes = body.ToArray();
        var message = Encoding.UTF8.GetString(bytes);

        _logger.LogDebug(
            "[RabbitMqSubscriberBase] 📥 Raw message received - Queue: {QueueName}, RoutingKey: {RoutingKey}, Size: {Size} bytes",
            _subscriberConfig.QueueName,
            routingKey,
            bytes.Length);

        return message;
    }

    /// <summary>
    /// Deserializes the specified message into an OrderEvent instance.
    /// </summary>
    /// <remarks>If deserialization fails, a warning is logged including the queue name and delivery tag for
    /// diagnostic purposes.</remarks>
    /// <param name="message">The JSON-formatted string representing the order event to deserialize. Cannot be null.</param>
    /// <param name="deliveryTag">The delivery tag associated with the message, used for logging and message tracking.</param>
    /// <returns>An OrderEvent object if deserialization succeeds; otherwise, null.</returns>
    private OrderEvent? DeserializeMessage(string message, ulong deliveryTag)
    {
        var orderEvent = JsonSerializer.Deserialize<OrderEvent>(message);

        if (orderEvent != null)
        {
            _logger.LogInformation(
                "[RabbitMqSubscriberBase] 📨 Message deserialized - Queue: {QueueName}, OrderId: {OrderId}, EventType: {EventType}",
                _subscriberConfig.QueueName,
                orderEvent.OrderId,
                orderEvent.EventType);
        }
        else
        {
            _logger.LogWarning(
                "[RabbitMqSubscriberBase] ⚠️ Failed to deserialize message - Queue: {QueueName}, DeliveryTag: {DeliveryTag}",
                _subscriberConfig.QueueName,
                deliveryTag);
        }

        return orderEvent;
    }


    /// <summary>
    /// Processes the specified order event and acknowledges its receipt to the message broker.
    /// </summary>
    /// <remarks>This method processes the message and sends an acknowledgment to the broker to confirm
    /// successful handling. If the acknowledgment is not sent, the message may be redelivered depending on broker
    /// configuration.</remarks>
    /// <param name="orderEvent">The order event to process. Cannot be null.</param>
    /// <param name="deliveryTag">The delivery tag that uniquely identifies the message to acknowledge in the message broker.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task ProcessAndAcknowledgeMessage(OrderEvent orderEvent, ulong deliveryTag)
    {
        await ProcessMessageAsync(orderEvent);

        _channel!.BasicAck(deliveryTag: deliveryTag, multiple: false);

        _logger.LogInformation(
            "[RabbitMqSubscriberBase] ✅ Message acknowledged - Queue: {QueueName}, OrderId: {OrderId}",
            _subscriberConfig.QueueName,
            orderEvent.OrderId);
    }


    /// <summary>
    /// Rejects a message from the queue and optionally requests that it be requeued for delivery.
    /// </summary>
    /// <remarks>Use this method to indicate that a message cannot be processed and should not be
    /// acknowledged. If <paramref name="requeue"/> is <see langword="true"/>, the message will be made available for
    /// redelivery; otherwise, it will be removed from the queue. This operation is typically used in message processing
    /// scenarios where certain messages must be retried or discarded based on application logic.</remarks>
    /// <param name="deliveryTag">The unique identifier of the message to reject. This value is assigned by the message broker and must correspond
    /// to a valid, unacknowledged message.</param>
    /// <param name="requeue">Specifies whether the rejected message should be requeued. Set to <see langword="true"/> to return the message
    /// to the queue; otherwise, <see langword="false"/> to discard it.</param>
    private void RejectMessage(ulong deliveryTag, bool requeue)
    {
        // Reject the message using BasicNack method with requeue option. This tells the broker whether to requeue the message or discard it.
        _channel!.BasicNack(deliveryTag: deliveryTag, multiple: false, requeue: requeue);

        // Check requeue flag and log appropriate message
        if (requeue)
        {
            _logger.LogWarning(
                "[RabbitMqSubscriberBase] ⚠️ Message rejected and requeued - DeliveryTag: {DeliveryTag}, Requeue: {Requeue}", 
                deliveryTag,
                requeue);
            return;
        }

            _logger.LogWarning(
                "[RabbitMqSubscriberBase] ⚠️ Message rejected without requeue and discarded - DeliveryTag: {DeliveryTag}, Requeue: {Requeue}",
                deliveryTag,
                requeue);


    }


    /// <summary>
    /// Handles a JSON deserialization error by logging the exception and rejecting the associated message without
    /// requeuing.
    /// </summary>
    /// <remarks>This method ensures that messages with invalid JSON are not requeued, preventing repeated
    /// processing of malformed data. The error is logged for diagnostic purposes.</remarks>
    /// <param name="jsonEx">The exception that was thrown during JSON deserialization. Cannot be null.</param>
    /// <param name="deliveryTag">The delivery tag that uniquely identifies the message to be rejected.</param>
    private void HandleJsonError(JsonException jsonEx, ulong deliveryTag)
    {
        _logger.LogError(
            jsonEx,
            "[RabbitMqSubscriberBase] ❌ JSON deserialization error - Queue: {QueueName}, DeliveryTag: {DeliveryTag}",
            _subscriberConfig.QueueName,
            deliveryTag);

        RejectMessage(deliveryTag, requeue: false);
    }

    /// <summary>
    /// Handles an error that occurs during message processing by logging the exception and requeuing the affected
    /// message.
    /// </summary>
    /// <remarks>This method ensures that messages which fail processing are not lost and can be retried by
    /// requeuing them. The error details are logged for diagnostic purposes.</remarks>
    /// <param name="ex">The exception that was thrown during message processing.</param>
    /// <param name="deliveryTag">The delivery tag that uniquely identifies the message to be requeued.</param>
    /// <param name="routingKey">The routing key associated with the message that caused the error.</param>
    private void HandleProcessingError(Exception ex, ulong deliveryTag, string routingKey)
    {
        _logger.LogError(
            ex,
            "[RabbitMqSubscriberBase] ❌ Error processing message - Queue: {QueueName}. Message will be requeued.",
            _subscriberConfig.QueueName);

        RejectMessage(deliveryTag, requeue: true); // Requeue the message for later processing
    }

    /// <summary>
    /// Abstract method to process the specified order event message.
    /// Must be implemented by derived classes to define custom processing logic.
    /// </summary>
    /// <param name="orderEvent">The order event to be processed. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected abstract Task ProcessMessageAsync(OrderEvent orderEvent);

    public override void Dispose()
    {
        _logger.LogInformation(
            "[RabbitMqSubscriberBase] 🔌 Disposing subscriber resources - Queue: {QueueName}",
            _subscriberConfig.QueueName);
        // Clean up RabbitMQ resources from IDisposable implementation
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        
        base.Dispose();
    }
}
