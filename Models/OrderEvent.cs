namespace OrderFlow.Core.Models;

/// <summary>
/// Represents an event that occurs during an order's lifecycle in the system.
/// </summary>
/// <remarks>
/// This class is used to communicate order state changes across different services
/// through RabbitMQ message broker. It contains the event metadata, type, and the
/// complete order data at the time of the event. Events are published and consumed
/// asynchronously, enabling loose coupling between services.
/// </remarks>
public class OrderEvent
{
    /// <summary>
    /// Gets or sets the unique identifier of the order associated with the event.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Describes what happened to the order.(business event classification)
    /// Gets or sets the type of the event. ( e.g., "OrderCreated", "OrderShipped" ).
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the order data associated with the event.
    /// </summary>
    public Order? OrderData { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the message providing additional information about the event.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
