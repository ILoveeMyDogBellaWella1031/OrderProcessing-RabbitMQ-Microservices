namespace OrderFlow.Core.Contracts.Responses;

/// <summary>
/// Represents the response returned after creating an order, containing the associated model of the specified type.
/// </summary>
/// <typeparam name="T">The type of the model included in the response.</typeparam>
public class CreateResponseDto<T>
{
    /// <summary>
    /// Gets or sets the model associated with the order. The model type is defined by the generic parameter.
    /// </summary>
    public T Model { get; set; } = default!; // Using generic type T to allow flexibility in the order model type
}
