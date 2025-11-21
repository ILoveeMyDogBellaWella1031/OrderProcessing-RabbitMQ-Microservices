using RabbitMQ.Client;

namespace OrderFlow.Core.Infrastructure.RabbitMQ;

/// <summary>
/// Factory interface for creating RabbitMQ connections.
/// </summary>
/// <remarks>
/// This interface abstracts the creation of RabbitMQ connections, allowing for easier
/// testing and dependency injection. Implementations should handle connection configuration,
/// error handling, and connection recovery settings.
/// </remarks>
public interface IRabbitMqConnectionFactory
{
    /// <summary>
    /// Creates and returns a new RabbitMQ connection.
    /// </summary>
    /// <returns>An active <see cref="IConnection"/> to RabbitMQ.</returns>
    IConnection CreateConnection();
}
