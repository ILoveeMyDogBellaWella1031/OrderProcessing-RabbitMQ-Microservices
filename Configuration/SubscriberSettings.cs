namespace OrderFlow.Core.Configuration;

/// <summary>
/// Configuration settings for RabbitMQ subscribers including queue names and routing keys.
/// </summary>
public class SubscriberSettings
{
    /// <summary>
    /// Gets or sets the configuration settings used for order processing subscribers.
    /// </summary>
    public SubscriberConfig OrderProcessing { get; set; } = null!;
    /// <summary>
    /// Gets or sets the configuration settings for subscriber notifications.
    /// </summary>
    public SubscriberConfig Notification { get; set; } = null!;
    /// <summary>
    /// Gets or sets the configuration settings used for payment verification of the subscriber.
    /// </summary>
    public SubscriberConfig PaymentVerification { get; set; } = null!;
    /// <summary>
    /// Gets or sets the configuration settings used for shipping subscribers.
    /// </summary>
    public SubscriberConfig Shipping { get; set; } = null!;
}
