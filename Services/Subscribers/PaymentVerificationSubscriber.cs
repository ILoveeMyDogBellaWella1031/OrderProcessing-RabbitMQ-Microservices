using Microsoft.Extensions.Options;
using OrderFlow.Core.Configuration;
using OrderFlow.Core.Infrastructure.RabbitMQ;
using OrderFlow.Core.Models;

namespace OrderFlow.Core.Services.Subscribers;

/// <summary>
/// Subscriber that handles payment verification for orders.
/// </summary>
/// <remarks>
/// This subscriber listens to all payment-related events (using wildcard pattern "payment.*")
/// and performs payment verification logic. In a production system, this would typically:
/// - Integrate with payment gateways (Stripe, PayPal, etc.)
/// - Verify transaction status
/// - Handle fraud detection
/// - Update order status based on payment results
/// - Trigger refund processes if needed
/// The subscriber uses a wildcard routing key to capture all payment events.
/// </remarks>
public class PaymentVerificationSubscriber : RabbitMqSubscriberBase
{
    protected override string QueueName => "payment_verification_queue";
    protected override string RoutingKey => "payment.*";

    public PaymentVerificationSubscriber(
        IRabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqSettings> settings,
        ILogger<PaymentVerificationSubscriber> logger)
        : base(connectionFactory, settings, logger)
    {
    }

    protected override async Task ProcessMessageAsync(OrderEvent orderEvent)
    {
        Logger.LogInformation("Verifying payment for order: {OrderId}", orderEvent.OrderId);
        
        // Simulate payment verification logic
        await Task.Delay(1500); // Simulate some work
        
        var isPaymentValid = orderEvent.OrderData?.TotalAmount > 0;
        
        if (isPaymentValid)
        {
            Logger.LogInformation(
                "Payment verified for order {OrderId}. Amount: {Amount:C}",
                orderEvent.OrderId,
                orderEvent.OrderData?.TotalAmount);
        }
        else
        {
            Logger.LogWarning("Payment verification failed for order {OrderId}", orderEvent.OrderId);
        }
    }
}
