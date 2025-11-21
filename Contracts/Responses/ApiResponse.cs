namespace OrderFlow.Core.Contracts.Responses;

/// <summary>
/// Generic API response wrapper that provides a consistent response structure across all endpoints.
/// </summary>
/// <typeparam name="T">The type of data being returned in the response.</typeparam>
/// <remarks>
/// This generic response pattern ensures:
/// - Consistent response structure across all API endpoints
/// - Clear success/failure indication
/// - Standardized error messaging
/// - Type-safe data payloads
/// - Better client-side error handling
/// </remarks>
public class ApiResponse<T>
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the status message describing the result of the operation.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data payload returned by the operation.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Gets or sets the list of error messages if the operation failed.
    /// </summary>
    public List<string>? Errors { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the response was generated.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful response with data.
    /// </summary>
    /// <param name="data">The data to return.</param>
    /// <param name="message">Optional success message. Otherwise defaults to a standard success message.</param>
    /// <returns>A successful API response.</returns>
    public static ApiResponse<T> SuccessResponse(T data, string message = "Operation completed successfully")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a failure response with error messages.
    /// </summary>
    /// <param name="message">The main error message.</param>
    /// <param name="errors">Optional list of detailed error messages.</param>
    /// <returns>A failed API response.</returns>
    public static ApiResponse<T> FailureResponse(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a failure response with a single error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="error">A single error detail.</param>
    /// <returns>A failed API response.</returns>
    public static ApiResponse<T> FailureResponse(string message, string error)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = new List<string> { error },
            Timestamp = DateTime.UtcNow
        };
    }
}
