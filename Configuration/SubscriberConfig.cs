namespace OrderFlow.Core.Configuration;

/// <summary>
/// Represents the configuration settings for an individual subscriber, including the queue name and routing key
/// pattern.
/// </summary>
/// <remarks>Use this class to specify the queue and routing key that a subscriber should listen to when binding
/// to a message broker exchange. This configuration is typically required when setting up message consumers in
/// distributed messaging systems.</remarks>
public class SubscriberConfig
{
    /// <summary>
    /// Gets or sets the name of the queue this subscriber listens to.
    /// </summary>
    public string QueueName { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the routing key pattern used to bind the queue to the exchange.
    /// </summary>
    public string RoutingKey { get; set; } = null!;
}
