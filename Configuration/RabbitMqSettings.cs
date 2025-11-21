namespace OrderFlow.Core.Configuration;

/// <summary>
/// Configuration settings for RabbitMQ message broker connection and exchange setup.
/// </summary>
/// <remarks>
/// This class contains all the necessary configuration parameters to establish a connection
/// to RabbitMQ and configure the exchange for message publishing and subscribing.
/// Default values are provided for local development environment.
/// These settings are typically bound from appsettings.json configuration file.
/// </remarks>
public class RabbitMqSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    /// <summary>
    /// Gets or sets the name of the exchange to which orders are published. Exchange in RabbitMQ is a message routing mechanism that determines how messages are directed to queues based on routing keys.
    /// </summary>
    public string ExchangeName { get; set; } = "order_exchange";
    /// <summary>
    /// Gets or sets the type of exchange to use for message routing.
    /// </summary>
    /// <remarks>Common exchange types include "direct", "fanout", "topic", and "headers". The exchange type
    /// determines how messages are routed to queues. Ensure that the specified exchange type is supported by the
    /// messaging broker in use.</remarks>
    public string ExchangeType { get; set; } = "topic";
}
