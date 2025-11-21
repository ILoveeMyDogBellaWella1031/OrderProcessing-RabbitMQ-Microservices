using Microsoft.Extensions.Options;
using OrderFlow.Core.Configuration;
using OrderFlow.Core.Infrastructure.RabbitMQ;
using OrderFlow.Core.Models;

namespace OrderFlow.Core.Services.Subscribers;

/// <summary>
/// Subscriber that processes order shipping events.
/// </summary>
/// <remarks>
/// This subscriber handles order.shipped events and manages shipping-related operations.
/// In a real-world scenario, this would typically:
/// - Integrate with shipping carriers (FedEx, UPS, DHL, etc.)
/// - Generate shipping labels and tracking numbers
/// - Calculate delivery estimates
/// - Update order status to shipped
/// - Notify customers about shipment details
/// The subscriber simulates shipping processing with appropriate delays to represent real operations.
/// </remarks>
public class ShippingSubscriber : RabbitMqSubscriberBase
{
    protected override string QueueName => "shipping_queue";
    protected override string RoutingKey => "order.shipped";

    public ShippingSubscriber(
        IRabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqSettings> settings,
        ILogger<ShippingSubscriber> logger)
        : base(connectionFactory, settings, logger)
    {
    }

    protected override async Task ProcessMessageAsync(OrderEvent orderEvent)
    {
        Logger.LogInformation("Processing shipment for order: {OrderId}", orderEvent.OrderId);
        
        // Simulate shipping processing logic
        await Task.Delay(2000); // Simulate some work
        
        Logger.LogInformation(
            "Shipment processed for order {OrderId}. Sending notification to customer {CustomerName}",
            orderEvent.OrderId,
            orderEvent.OrderData?.CustomerName);
    }
}
