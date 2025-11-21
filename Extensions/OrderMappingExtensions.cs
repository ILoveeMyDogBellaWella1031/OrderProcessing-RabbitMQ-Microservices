using OrderFlow.Core.Contracts.Responses;
using OrderFlow.Core.Models;

namespace OrderFlow.Core.Extensions;

/// <summary>
/// Extension methods for mapping domain models to response DTOs.
/// </summary>
/// <remarks>
/// These extensions provide clean, reusable mapping logic to convert internal
/// domain models to API response DTOs, following the principle of separation
/// between domain and API layers.
/// </remarks>
public static class MappingExtensions
{
    /// <summary>
    /// Maps an Order domain model to OrderResponseDto.
    /// </summary>
    /// <param name="order">The order domain model to map.</param>
    /// <returns>A new OrderResponseDto instance with mapped properties.</returns>
    public static OrderResponseDto ToResponseDto(this Order order)
    {
        return new OrderResponseDto
        {
            Id = order.Id,
            CustomerName = order.CustomerName,
            ProductName = order.ProductName,
            Quantity = order.Quantity,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString(),
            CreatedAt = order.CreatedAt
        };
    }

    /// <summary>
    /// Extension method to map an Order domain model to CreateOrderResponseDto.
    /// This method extends the mapping capabilities by wrapping the existing ToResponseDto method.
    /// As parameter it takes an Order instance (extended type) and returns a CreateOrderResponseDto instance.
    /// </summary>
    /// <param name="order">The order domain model to map.</param>
    /// <returns>A new CreateOrderResponseDto instance with mapped order details.</returns>
    /// <remarks>
    /// This extension method encapsulates the creation response structure,
    /// making it easy to add creation-specific metadata in the future
    /// (e.g., event ID, queue position, processing estimates).
    /// </remarks>
    public static CreateResponseDto ToCreateResponseDto(this Order order)
    {
        return new CreateResponseDto
        {
            Order = order.ToResponseDto()
        };
    }
}
