using Microsoft.Extensions.Options;
using OrderFlow.Core.Configuration;
using OrderFlow.Core.Infrastructure.RabbitMQ;
using OrderFlow.Core.Models;

namespace OrderFlow.Core.Services.Subscribers;

/// <summary>
/// Subscriber that processes newly created order events.
/// </summary>
/// <remarks>
/// This subscriber listens to order.created events and handles initial order processing logic.
/// It simulates business logic that would typically include:
/// - Validating order details
/// - Checking inventory availability
/// - Calculating shipping costs
/// - Initiating payment processing
/// The subscriber runs as a background service and processes messages from the order_processing_queue.
/// </remarks>
public class OrderProcessingSubscriber : RabbitMqSubscriberBase
{
    protected override string QueueName => "order_processing_queue";
    protected override string RoutingKey => "order.created";

    public OrderProcessingSubscriber(
        IRabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqSettings> settings,
        ILogger<OrderProcessingSubscriber> logger)
        : base(connectionFactory, settings, logger)
    {
    }

    protected override async Task ProcessMessageAsync(OrderEvent orderEvent)
    {
        Logger.LogInformation("Processing order: {OrderId}", orderEvent.OrderId);
        
        // Simulate order processing logic
        await Task.Delay(1000); // Simulate some work
        
        Logger.LogInformation(
            "Order {OrderId} processed successfully. Customer: {CustomerName}, Product: {ProductName}",
            orderEvent.OrderId,
            orderEvent.OrderData?.CustomerName,
            orderEvent.OrderData?.ProductName);
    }
}
