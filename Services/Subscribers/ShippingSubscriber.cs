using Microsoft.Extensions.Options;
using OrderFlow.Core.Configuration;
using OrderFlow.Core.Infrastructure.RabbitMQ;
using OrderFlow.Core.Models;

namespace OrderFlow.Core.Services.Subscribers;

/// <summary>
/// Subscriber that processes order shipping events.
/// </summary>
/// <remarks>
/// This subscriber handles order.shipped events (configured in appsettings.json)
/// and manages shipping-related operations. In a real-world scenario, this would typically:
/// - Integrate with shipping carriers (FedEx, UPS, DHL, etc.)
/// - Generate shipping labels and tracking numbers
/// - Calculate delivery estimates
/// - Update order status to shipped
/// - Notify customers about shipment details
/// The subscriber simulates shipping processing with appropriate delays to represent real operations.
/// 
/// Configuration: RabbitMq:Subscribers:Shipping in appsettings.json
/// </remarks>
public class ShippingSubscriber : RabbitMqSubscriberBase
{
    protected override string ConfigurationKey => "Shipping"; // Matches the configuration section for this subscriber

    public ShippingSubscriber(
        IRabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqSettings> settings,
        ILogger<ShippingSubscriber> logger)
        : base(connectionFactory, settings, logger)
    {
    }

    protected override async Task ProcessMessageAsync(OrderEvent orderEvent)
    {
        _logger.LogInformation(
            "[ShippingSubscriber] 📦 Received shipping event for processing - OrderId: {OrderId}, EventType: {EventType}",
            orderEvent.OrderId,
            orderEvent.EventType);

        // Simulate shipping processing logic (e.g., generating labels, updating status, notifying carriers). Implementation would go here.
        await Task.Delay(2000);
        
        _logger.LogInformation(
            "[ShippingSubscriber] 🚚 Shipment processed successfully - OrderId: {OrderId}, Customer: {CustomerName}, Product: {ProductName}, Quantity: {Quantity}",
            orderEvent.OrderId,
            orderEvent.OrderData?.CustomerName,
            orderEvent.OrderData?.ProductName,
            orderEvent.OrderData?.Quantity);
    }
}
