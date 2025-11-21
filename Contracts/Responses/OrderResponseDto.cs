namespace OrderFlow.Core.Contracts.Responses;

/// <summary>
/// Response model representing order details.
/// </summary>
/// <remarks>
/// This DTO is used to return order information to clients without exposing
/// internal domain models. It contains all relevant order data in a format
/// suitable for API responses.
/// </remarks>
public class OrderResponseDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the order.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity ordered.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the total amount for the order.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the current status of the order.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the order was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
