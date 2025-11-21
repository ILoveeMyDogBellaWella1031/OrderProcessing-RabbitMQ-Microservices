namespace OrderFlow.Core.Contracts.Requests;

/// <summary>
/// Request model for creating a new order.
/// </summary>
/// <remarks>
/// This class represents the data required from the client to create a new order.
/// It contains only the essential information needed; server-side values like
/// Order ID, Status, and CreatedAt timestamp are generated automatically.
/// </remarks>
public class CreateOrderRequestDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
}
