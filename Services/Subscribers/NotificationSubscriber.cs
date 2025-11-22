using Microsoft.Extensions.Options;
using OrderFlow.Core.Configuration;
using OrderFlow.Core.Infrastructure.RabbitMQ;
using OrderFlow.Core.Models;

namespace OrderFlow.Core.Services.Subscribers;

/// <summary>
/// Subscriber that sends notifications for all order events.
/// </summary>
/// <remarks>
/// This subscriber listens to all order-related events (configured in appsettings.json)
/// and sends appropriate notifications to customers. It demonstrates a fan-out pattern
/// where one subscriber handles multiple event types. In production, this would typically:
/// - Send email notifications using services like SendGrid or AWS SES
/// - Send SMS notifications via Twilio or similar services
/// - Push notifications to mobile apps
/// - Post updates to customer portals
/// The notification type is determined based on the event type, allowing for customized
/// messages for different order lifecycle stages.
/// 
/// Configuration: RabbitMq:Subscribers:Notification in appsettings.json
/// </remarks>


// any class that inherits from RabbitMqSubscriberBase must implement ProcessMessageAsync. This is where the logic for handling incoming messages is defined.
// Instanciation Sequence is as follows:
// 1. When the application starts, the NotificationSubscriber is registered as a hosted service in the dependency injection container.
// - Constructor Injection is used to provide the necessary dependencies: IRabbitMqConnectionFactory, IOptions<RabbitMqSettings>, and ILogger<NotificationSubscriber>.
// - The dependency injection container resolves these dependencies and creates an instance of NotificationSubscriber executing the constructor inmediately.
// - The base class constructor (RabbitMqSubscriberBase) is called, which initializes the RabbitMQ connection and prepares to listen for messages based on the configuration specified by ConfigurationKey.
// - Method executeAsync in the base class RabbitMqSubscriberBase is called to start listening for messages.
// 2. When a message arrives in the RabbitMQ queue that this subscriber is listening to:
// - The RabbitMqSubscriberBase receives the message and deserializes it into an OrderEvent object.
// - The ProcessMessageAsync method in NotificationSubscriber is invoked with the deserialized OrderEvent.
// - The logic defined in ProcessMessageAsync is executed, which includes logging the receipt of the event, simulating notification sending, and logging the successful sending of the notification.
// - After processing, the base class handles message acknowledgment to RabbitMQ, ensuring that the message is marked as processed.
// - If an error occurs during processing, the base class can handle retries or dead-lettering based on its implementation.
// 3. This cycle repeats for each incoming message until the application is stopped or the subscriber is disposed of.


public class NotificationSubscriber : RabbitMqSubscriberBase
{
    protected override string ConfigurationKey => "Notification"; // Matches the configuration section for this subscriber. That's it! everything else is handled by the base class.

    public NotificationSubscriber(
        IRabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqSettings> settings,
        ILogger<NotificationSubscriber> logger)
        : base(connectionFactory, settings, logger)
    {
    }

    protected override async Task ProcessMessageAsync(OrderEvent orderEvent)
    {
        _logger.LogInformation(
            "[NotificationSubscriber] 📬 Received order event for notification - OrderId: {OrderId}, EventType: {EventType}",
            orderEvent.OrderId,
            orderEvent.EventType);

        // Simulate notification sending logic (email, SMS, push notification, etc.).Implementation would go here. In this example, we just simulate a delay.
        await Task.Delay(500);
        
        var notificationType = orderEvent.EventType switch
        {
            OrderEventTypes.OrderCreated => "Order Confirmation",
            OrderEventTypes.PaymentVerified => "Payment Confirmation",
            OrderEventTypes.OrderShipped => "Shipping Notification",
            OrderEventTypes.OrderDelivered => "Delivery Confirmation",
            OrderEventTypes.OrderCancelled => "Cancellation Notice",
            _ => "Order Update"
        };
        
        _logger.LogInformation(
            "[NotificationSubscriber] ✉️ Notification sent successfully - Type: {NotificationType}, OrderId: {OrderId}, Customer: {CustomerName}, Product: {ProductName}",
            notificationType,
            orderEvent.OrderId,
            orderEvent.OrderData?.CustomerName,
            orderEvent.OrderData?.ProductName);
    }
}
