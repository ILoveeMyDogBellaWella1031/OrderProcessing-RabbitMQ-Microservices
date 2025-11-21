using Microsoft.Extensions.Options;
using OrderFlow.Core.Configuration;
using OrderFlow.Core.Infrastructure.RabbitMQ;
using OrderFlow.Core.Models;

namespace OrderFlow.Core.Services.Subscribers;

/// <summary>
/// Subscriber that sends notifications for all order events.
/// </summary>
/// <remarks>
/// This subscriber listens to all order-related events (using wildcard pattern "order.*")
/// and sends appropriate notifications to customers. It demonstrates a fan-out pattern
/// where one subscriber handles multiple event types. In production, this would typically:
/// - Send email notifications using services like SendGrid or AWS SES
/// - Send SMS notifications via Twilio or similar services
/// - Push notifications to mobile apps
/// - Post updates to customer portals
/// The notification type is determined based on the event type, allowing for customized
/// messages for different order lifecycle stages.
/// </remarks>
public class NotificationSubscriber : RabbitMqSubscriberBase
{
    protected override string QueueName => "notification_queue";
    protected override string RoutingKey => "order.*"; // Subscribe to all order events

    public NotificationSubscriber(
        IRabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqSettings> settings,
        ILogger<NotificationSubscriber> logger)
        : base(connectionFactory, settings, logger)
    {
    }

    /// <summary>
    /// Processes the received order event and sends a notification.
    /// </summary>
    /// <param name="orderEvent"></param>
    /// <returns></returns>
    protected override async Task ProcessMessageAsync(OrderEvent orderEvent)
    {
        Logger.LogInformation(
            "Sending notification for order: {OrderId}, Event: {EventType}",
            orderEvent.OrderId,
            orderEvent.EventType);

        // Simulate notification sending logic (email, SMS, push notification, etc.). Delayd to mimic async operation.
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
        
        Logger.LogInformation(
            "Notification sent: {NotificationType} for order {OrderId} to {CustomerName}",
            notificationType,
            orderEvent.OrderId,
            orderEvent.OrderData?.CustomerName);
    }
}
