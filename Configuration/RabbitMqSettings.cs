namespace OrderFlow.Core.Configuration;

/// <summary>
/// Configuration settings for RabbitMQ message broker connection and exchange setup.
/// </summary>
/// <remarks>
/// This class contains all the necessary configuration parameters to establish a connection
/// to RabbitMQ and configure the exchange for message publishing and subscribing.
/// These settings are bound from appsettings.json configuration file.
/// </remarks>
public class RabbitMqSettings
{
    public string HostName { get; set; } = null!;
    public int Port { get; set; }
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the name of the exchange to which orders are published.
    /// </summary>
    public string ExchangeName { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the type of exchange to use for message routing (e.g., "topic", "direct", "fanout").
    /// </summary>
    public string ExchangeType { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the subscriber-specific configuration including queue names and routing keys.
    /// </summary>
    public SubscriberSettings Subscribers { get; set; } = null!;
}
