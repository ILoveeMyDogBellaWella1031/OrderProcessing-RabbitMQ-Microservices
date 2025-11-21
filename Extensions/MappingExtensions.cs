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
    /// Generic extension method to map any domain model to a DTO using a provided mapping function.
    /// </summary>
    /// <typeparam name="TSource">The source domain model type.</typeparam>
    /// <typeparam name="TDestination">The destination DTO type.</typeparam>
    /// <param name="source">The source domain model to map.</param>
    /// <param name="mapper">A function that defines how to map from TSource to TDestination.</param>
    /// <returns>A new TDestination instance with mapped properties.</returns>
    /// <remarks>
    /// This generic method provides maximum flexibility for mapping any type to any other type.
    /// 
    /// Example usage:
    /// <code>
    /// var dto = order.MapTo&lt;Order, OrderResponseDto&gt;(o => new OrderResponseDto
    /// {
    ///     Id = o.Id,
    ///     CustomerName = o.CustomerName,
    ///     // ... other properties
    /// });
    /// </code>
    /// </remarks>
    public static TDestination MapTo<TSource, TDestination>(
        this TSource source, 
        Func<TSource, TDestination> mapper)
        where TSource : class
        where TDestination : class
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        ArgumentNullException.ThrowIfNull(mapper, nameof(mapper));

        return mapper(source);
    }

    /// <summary>
    /// Generic extension method to map a collection of domain models to a collection of DTOs.
    /// </summary>
    /// <typeparam name="TSource">The source domain model type.</typeparam>
    /// <typeparam name="TDestination">The destination DTO type.</typeparam>
    /// <param name="source">The source collection to map.</param>
    /// <param name="mapper">A function that defines how to map from TSource to TDestination.</param>
    /// <returns>An IEnumerable of TDestination with mapped properties.</returns>
    /// <remarks>
    /// This method simplifies mapping collections of domain models to DTOs.
    /// 
    /// Example usage:
    /// <code>
    /// var dtos = orders.MapToList&lt;Order, OrderResponseDto&gt;(o => new OrderResponseDto
    /// {
    ///     Id = o.Id,
    ///     CustomerName = o.CustomerName,
    ///     // ... other properties
    /// });
    /// </code>
    /// </remarks>
    public static IEnumerable<TDestination> MapToList<TSource, TDestination>(
        this IEnumerable<TSource> source, 
        Func<TSource, TDestination> mapper)
        where TSource : class
        where TDestination : class
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        ArgumentNullException.ThrowIfNull(mapper, nameof(mapper));

        return source.Select(mapper);
    }

    /// <summary>
    /// Maps an Order domain model to OrderResponseDto.
    /// </summary>
    /// <param name="order">The order domain model to map.</param>
    /// <returns>A new OrderResponseDto instance with mapped properties.</returns>
    /// <remarks>
    /// This is a convenience method that provides a specific implementation for Order mapping.
    /// It's kept for backward compatibility and ease of use.
    /// 
    /// Alternatively, you can use the generic MapTo method:
    /// <code>
    /// var dto = order.MapTo&lt;Order, OrderResponseDto&gt;(ToOrderResponseDto);
    /// </code>
    /// </remarks>
    public static OrderResponseDto ToResponseDto(this Order order)
    {
        ArgumentNullException.ThrowIfNull(order, nameof(order));

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
    /// Static mapping function for Order to OrderResponseDto conversion.
    /// This can be used with the generic MapTo method or as a standalone mapper.
    /// </summary>
    /// <param name="order">The order to map.</param>
    /// <returns>A new OrderResponseDto instance.</returns>
    public static OrderResponseDto ToOrderResponseDto(Order order)
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
    [Obsolete("This method is no longer used. Use ToResponseDto() directly for a simpler response structure. Will be removed in a future version.")]
    public static CreateResponseDto<OrderResponseDto> ToCreateResponseDto(this Order order)
    {
        return new CreateResponseDto<OrderResponseDto>
        {
            Model = order.ToResponseDto()
        };
    }
}
