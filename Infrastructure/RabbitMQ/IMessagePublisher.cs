using OrderFlow.Core.Models;

namespace OrderFlow.Core.Infrastructure.RabbitMQ;

/// <summary>
/// Interface for publishing order events to RabbitMQ message broker.
/// </summary>
/// <remarks>
/// This interface defines the contract for message publishing operations in the system.
/// Implementations should handle message serialization, connection management, and
/// delivery confirmation. The publisher uses routing keys to direct messages to the
/// appropriate queues through the configured exchange.
/// </remarks>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes an order event asynchronously to RabbitMQ with the specified routing key.
    /// </summary>
    /// <param name="orderEvent">The order event to publish. Used by consumers to understand what type of event occurred</param>
    /// <param name="routingKey">Controls message routing in RabbitMQ (infrastructure concern).The routing key to use for message routing. Determines which queue(s) receive the message based on exchange type and bindings</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync(OrderEvent orderEvent, string routingKey);
}
