namespace OrderFlow.Core.Models;

/// <summary>
/// Defines constant values for all order event types used in the system.
/// </summary>
/// <remarks>
/// This static class provides a centralized definition of all order event types
/// to ensure consistency across publishers and subscribers. The event types follow
/// a dot notation convention (category.action) which aligns with RabbitMQ topic
/// exchange routing patterns. Using these constants prevents typos and makes it
/// easier to maintain event type definitions across the application.
/// </remarks>
public static class OrderEventTypes
{
    /// <summary>
    /// The order has been created. Values: "order.created"
    /// </summary>
    public const string OrderCreated = "order.created";

    /// <summary>
    /// The order is being processed. Values: "order.processing"
    /// </summary>
    public const string OrderProcessing = "order.processing";

    /// <summary>
    /// The payment for the order has been verified. Values: "payment.verified"
    /// </summary>
    public const string PaymentVerified = "payment.verified";

    /// <summary>
    /// The order has been shipped. Values: "order.shipped"
    /// </summary>
    public const string OrderShipped = "order.shipped";

    /// <summary>
    /// The order has been delivered. Values: "order.delivered"
    /// </summary>
    public const string OrderDelivered = "order.delivered";

    /// <summary>
    /// The order has been cancelled. Values: "order.cancelled"
    /// </summary>
    public const string OrderCancelled = "order.cancelled";
}
