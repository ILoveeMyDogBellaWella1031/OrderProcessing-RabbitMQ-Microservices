namespace OrderFlow.Core.Contracts.Responses;

/// <summary>
/// Data model for order operation results (payment, shipping, delivery, cancellation).
/// </summary>
/// <remarks>
/// This DTO contains the result data for operations that publish events.
/// It is wrapped in an ApiResponse&lt;OrderOperationResponseDto&gt; when returned from the API.
/// </remarks>
public class OrderOperationResponseDto
{
    /// <summary>
    /// Gets or sets the order ID associated with the operation.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the type of event that was published.
    /// </summary>
    public string EventType { get; set; } = string.Empty;
}
