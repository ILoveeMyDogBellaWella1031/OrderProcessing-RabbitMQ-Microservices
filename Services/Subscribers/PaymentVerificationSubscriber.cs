using Microsoft.Extensions.Options;
using OrderFlow.Core.Configuration;
using OrderFlow.Core.Infrastructure.RabbitMQ;
using OrderFlow.Core.Models;

namespace OrderFlow.Core.Services.Subscribers;

/// <summary>
/// Subscriber that handles payment verification for orders.
/// </summary>
/// <remarks>
/// This subscriber listens to all payment-related events (configured in appsettings.json)
/// and performs payment verification logic. In a production system, this would typically:
/// - Integrate with payment gateways (Stripe, PayPal, etc.)
/// - Verify transaction status
/// - Handle fraud detection
/// - Update order status based on payment results
/// - Trigger refund processes if needed
/// 
/// Configuration: RabbitMq:Subscribers:PaymentVerification in appsettings.json
/// </remarks>
public class PaymentVerificationSubscriber : RabbitMqSubscriberBase
{
    protected override string ConfigurationKey => "PaymentVerification"; // Matches the configuration section for this subscriber

    public PaymentVerificationSubscriber(
        IRabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqSettings> settings,
        ILogger<PaymentVerificationSubscriber> logger)
        : base(connectionFactory, settings, logger)
    {
    }

    protected override async Task ProcessMessageAsync(OrderEvent orderEvent)
    {
        _logger.LogInformation(
            "[PaymentVerificationSubscriber] 💳 Received payment event for verification - OrderId: {OrderId}, EventType: {EventType}",
            orderEvent.OrderId,
            orderEvent.EventType);

        // Simulate payment verification logic(e.g., calling payment gateway APIs, checking transaction status, etc.). In a real system, this would involve more complex operations.
        await Task.Delay(1500);
        
        var isPaymentValid = orderEvent.OrderData?.TotalAmount > 0;
        
        if (isPaymentValid)
        {
            _logger.LogInformation(
                "[PaymentVerificationSubscriber] ✅ Payment verified successfully - OrderId: {OrderId}, Amount: {Amount:C}, Customer: {CustomerName}",
                orderEvent.OrderId,
                orderEvent.OrderData?.TotalAmount,
                orderEvent.OrderData?.CustomerName);
        }
        else
        {
            _logger.LogWarning(
                "[PaymentVerificationSubscriber] ⚠️ Payment verification failed - OrderId: {OrderId}, Amount: {Amount:C}, Customer: {CustomerName}",
                orderEvent.OrderId,
                orderEvent.OrderData?.TotalAmount,
                orderEvent.OrderData?.CustomerName);
        }
    }
}
