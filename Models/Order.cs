namespace OrderFlow.Core.Models;

/// <summary>
/// Represents an order entity in the order management system.
/// </summary>
/// <remarks>
/// This class contains all the essential information about a customer's order including
/// product details, pricing, status, and timestamps. Orders go through various status
/// states as defined in <see cref="OrderStatus"/> enum throughout their lifecycle.
/// The order entity is used both for storage and message passing in the event-driven architecture.
/// </remarks>
public class Order
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Defines the possible states of an order throughout its lifecycle. Represent Status transitions for an Order.
/// </summary>
/// <remarks>
/// Orders progress through these states sequentially in normal workflow:
/// Created ? Processing ? PaymentVerified ? Shipped ? Delivered.
/// An order can be Cancelled at any point before Delivered status.
/// </remarks>
public enum OrderStatus
{
    Created,
    Processing,
    PaymentVerified,
    Shipped,
    Delivered,
    Cancelled
}
