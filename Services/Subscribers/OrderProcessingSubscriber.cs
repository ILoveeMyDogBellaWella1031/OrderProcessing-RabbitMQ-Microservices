using Microsoft.Extensions.Options;
using OrderFlow.Core.Configuration;
using OrderFlow.Core.Infrastructure.RabbitMQ;
using OrderFlow.Core.Models;

namespace OrderFlow.Core.Services.Subscribers;

/// <summary>
/// Subscriber that processes newly created order events.
/// </summary>
/// <remarks>
/// This subscriber listens to order.created events (configured in appsettings.json)
/// and handles initial order processing logic. It simulates business logic that would typically include:
/// - Validating order details
/// - Checking inventory availability
/// - Calculating shipping costs
/// - Initiating payment processing
/// The subscriber runs as a background service and processes messages from the configured queue.
/// 
/// Configuration: RabbitMq:Subscribers:OrderProcessing in appsettings.json
/// </remarks>
public class OrderProcessingSubscriber : RabbitMqSubscriberBase
{
    protected override string ConfigurationKey => "OrderProcessing"; // Matches the configuration section for this subscriber

    public OrderProcessingSubscriber(
        IRabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqSettings> settings,
        ILogger<OrderProcessingSubscriber> logger)
        : base(connectionFactory, settings, logger)
    {
    }
    
    protected override async Task ProcessMessageAsync(OrderEvent orderEvent)
    {
        _logger.LogInformation(
            "[OrderProcessingSubscriber] 📨 Received order event for processing - OrderId: {OrderId}, EventType: {EventType}",
            orderEvent.OrderId,
            orderEvent.EventType);

        // Simulate order processing logic (e.g., validation, inventory check, payment initiation)
        await Task.Delay(1000);
        
        _logger.LogInformation(
            "[OrderProcessingSubscriber] ✅ Successfully processed order - OrderId: {OrderId}, Customer: {CustomerName}, Product: {ProductName}, Quantity: {Quantity}, Total: {TotalAmount:C}",
            orderEvent.OrderId,
            orderEvent.OrderData?.CustomerName,
            orderEvent.OrderData?.ProductName,
            orderEvent.OrderData?.Quantity,
            orderEvent.OrderData?.TotalAmount);
    }
}
