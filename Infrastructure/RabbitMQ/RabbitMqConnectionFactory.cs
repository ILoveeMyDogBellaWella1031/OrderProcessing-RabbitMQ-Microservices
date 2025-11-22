using Microsoft.Extensions.Options;
using OrderFlow.Core.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;

namespace OrderFlow.Core.Infrastructure.RabbitMQ;

/// <summary>
/// Concrete implementation of <see cref="IRabbitMqConnectionFactory"/> for creating RabbitMQ connections. 
/// Connection factory is used to establish connections to the RabbitMQ server based on provided settings.
/// - Implements automatic recovery and network recovery to handle connection interruptions.
/// - Logs all connection attempts and errors for monitoring and debugging purposes.
/// - Retries connection attempts with exponential backoff in case of failures.
/// </summary>
/// <remarks>
/// This factory creates RabbitMQ connections using the configuration settings provided through
/// <see cref="RabbitMqSettings"/>. It includes automatic recovery and network recovery features
/// to handle connection interruptions. All connection operations are logged for monitoring and debugging.
/// </remarks>
public class RabbitMqConnectionFactory : IRabbitMqConnectionFactory
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqConnectionFactory> _logger;
    private const int MaxRetryAttempts = 5;
    private const int InitialRetryDelayMs = 1000;

    public RabbitMqConnectionFactory(
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqConnectionFactory> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public IConnection CreateConnection()
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            RequestedConnectionTimeout = TimeSpan.FromSeconds(30),
            SocketReadTimeout = TimeSpan.FromSeconds(30),
            SocketWriteTimeout = TimeSpan.FromSeconds(30)
        };

        var retryDelay = InitialRetryDelayMs;

        for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        {
            try
            {
                _logger.LogInformation($"Attempting to connect to RabbitMQ at {_settings.HostName}:{_settings.Port} (Attempt {attempt}/{MaxRetryAttempts})", 
                    _settings.HostName, _settings.Port, attempt, MaxRetryAttempts);
                
                var connection = factory.CreateConnection();
                
                _logger.LogInformation("Successfully connected to RabbitMQ at {HostName}:{Port}", 
                    _settings.HostName, _settings.Port);
                
                return connection;
            }
            catch (BrokerUnreachableException ex)
            {
                _logger.LogWarning(ex, 
                    $"Failed to connect to RabbitMQ at {_settings.HostName}:{_settings.Port} (Attempt {attempt}/{MaxRetryAttempts}). " +
                    "Reason: {Reason}", 
                    _settings.HostName, _settings.Port, attempt, MaxRetryAttempts, ex.Message);

                if (attempt == MaxRetryAttempts)
                {
                    _logger.LogError(ex, 
                        "Failed to connect to RabbitMQ at {HostName}:{Port} after {MaxAttempts} attempts. " +
                        "Please ensure RabbitMQ is running and accessible.", 
                        _settings.HostName, _settings.Port, MaxRetryAttempts);
                    throw;
                }

                _logger.LogInformation("Waiting {Delay}ms before retry...", retryDelay);
                Thread.Sleep(retryDelay);
                retryDelay *= 2; // Exponential backoff
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error connecting to RabbitMQ at {_settings.HostName}:{_settings.Port}", 
                    _settings.HostName, _settings.Port);
                throw;
            }
        }

        throw new InvalidOperationException("Failed to create RabbitMQ connection after all retry attempts.");
    }
}
