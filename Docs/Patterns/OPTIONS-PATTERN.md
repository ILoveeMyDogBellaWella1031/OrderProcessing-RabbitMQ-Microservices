# 🎯 Options Pattern Implementation in OrderFlow.Core

## Overview

The Options Pattern is used in OrderFlow.Core to bind configuration from `appsettings.json` to strongly-typed C# classes, providing type-safe access to configuration throughout the application.

---

## 📋 Implementation Steps

### 1. **Define Configuration Classes**

**File**: `Configuration/RabbitMqSettings.cs`

```csharp
public class RabbitMqSettings
{
    public string HostName { get; set; } = null!;
    public int Port { get; set; }
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string ExchangeName { get; set; } = null!;
    public string ExchangeType { get; set; } = null!;
    public SubscriberSettings Subscribers { get; set; } = null!;
}

public class SubscriberSettings
{
    public SubscriberConfig OrderProcessing { get; set; } = null!;
    public SubscriberConfig Notification { get; set; } = null!;
    public SubscriberConfig PaymentVerification { get; set; } = null!;
    public SubscriberConfig Shipping { get; set; } = null!;
}

public class SubscriberConfig
{
    public string QueueName { get; set; } = null!;
    public string RoutingKey { get; set; } = null!;
}
```

**Key Points**:
- Properties match JSON structure (case-insensitive)
- `null!` indicates non-nullable reference that will be set by configuration
- Nested classes for hierarchical configuration

---

### 2. **Add Configuration to JSON**

**File**: `appsettings.Development.json`

```json
{
  "RabbitMq": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "admin",
    "Password": "admin123",
    "ExchangeName": "order_exchange",
    "ExchangeType": "topic",
    "Subscribers": {
      "OrderProcessing": {
        "QueueName": "order_processing_queue",
        "RoutingKey": "order.created"
      },
      "Notification": {
        "QueueName": "notification_queue",
        "RoutingKey": "order.*"
      }
    }
  }
}
```

---

### 3. **Register Configuration in DI Container**

**File**: `Program.cs`

```csharp
// Bind configuration section to options class
builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMq"));
```

**What This Does**:
1. Gets the "RabbitMq" section from all configuration sources
2. Binds JSON properties to `RabbitMqSettings` class properties
3. Registers as `IOptions<RabbitMqSettings>` in DI container

---

### 4. **Inject and Use Configuration**

**File**: `Infrastructure/RabbitMQ/RabbitMqSubscriberBase.cs`

```csharp
protected RabbitMqSubscriberBase(
    IRabbitMqConnectionFactory connectionFactory,
    IOptions<RabbitMqSettings> settings,  // ← Injected via DI
    ILogger logger)
{
    ConnectionFactory = connectionFactory;
    Settings = settings.Value;  // ← Extract actual settings object
    Logger = logger;
    
    // Access configuration
    _subscriberConfig = GetSubscriberConfig(ConfigurationKey);
}

private SubscriberConfig GetSubscriberConfig(string key)
{
    var config = key switch
    {
        "OrderProcessing" => Settings.Subscribers.OrderProcessing,
        "Notification" => Settings.Subscribers.Notification,
        // ... access nested configuration
    };
    
    return config;
}
```

---

## 🔄 Configuration Flow

```
┌────────────────────────────────────────────────────────────┐
│ 1. Application Startup                                      │
│    ✓ ASP.NET Core reads appsettings.json                   │
└─────────────────┬──────────────────────────────────────────┘
                  │
                  ▼
┌────────────────────────────────────────────────────────────┐
│ 2. Configuration Binding (Program.cs)                       │
│    builder.Services.Configure<RabbitMqSettings>(...)        │
│    ✓ JSON → RabbitMqSettings class                         │
│    ✓ Registered in DI as IOptions<RabbitMqSettings>        │
└─────────────────┬──────────────────────────────────────────┘
                  │
                  ▼
┌────────────────────────────────────────────────────────────┐
│ 3. Dependency Injection                                     │
│    new RabbitMqSubscriberBase(..., IOptions<Settings>)     │
│    ✓ DI resolves and injects IOptions<RabbitMqSettings>    │
└─────────────────┬──────────────────────────────────────────┘
                  │
                  ▼
┌────────────────────────────────────────────────────────────┐
│ 4. Extract Configuration                                    │
│    Settings = settings.Value                                │
│    ✓ Access strongly-typed configuration                   │
└────────────────────────────────────────────────────────────┘
```

---

## ✅ Benefits

| Benefit | Description |
|---------|-------------|
| **Type Safety** | Compile-time checking of configuration access |
| **IntelliSense** | IDE autocomplete for configuration properties |
| **Validation** | Can add data annotations for validation |
| **Testability** | Easy to mock `IOptions<T>` in unit tests |
| **Separation** | Configuration separate from business logic |
| **Environment-Specific** | Override per environment (Dev/Staging/Prod) |

---

## 🎯 Real-World Usage Example

### Accessing Configuration in Subscriber

```csharp
public class ShippingSubscriber : RabbitMqSubscriberBase
{
    protected override string ConfigurationKey => "Shipping";
    
    // Configuration automatically loaded from:
    // appsettings.json → RabbitMq:Subscribers:Shipping
}
```

### Configuration Used During Initialization

```csharp
private void InitializeInfrastructure()
{
    // Uses Settings.ExchangeName from JSON
    _channel.ExchangeDeclare(
        exchange: Settings.ExchangeName,  // "order_exchange"
        type: Settings.ExchangeType);      // "topic"
    
    // Uses _subscriberConfig.QueueName from JSON
    _channel.QueueDeclare(
        queue: _subscriberConfig.QueueName);  // "shipping_queue"
}
```

---

## 🔧 Configuration Priority

ASP.NET Core loads configuration from multiple sources:

```
1. Command-line arguments     (highest priority)
2. Environment variables
3. appsettings.{Environment}.json
4. appsettings.json           (lowest priority)
```

**Example Override**:

```json
// appsettings.json (base)
{
  "RabbitMq": {
    "HostName": "localhost"
  }
}

// appsettings.Production.json (override)
{
  "RabbitMq": {
    "HostName": "prod-rabbitmq.example.com"
  }
}
```

---

## 📝 Best Practices

### ✅ Do

1. **Use descriptive property names** that match JSON keys
2. **Mark required properties** with `= null!` or data annotations
3. **Validate configuration** on startup
4. **Use nested classes** for hierarchical configuration
5. **Document configuration** in code comments

### ❌ Don't

1. **Don't hardcode values** in configuration classes
2. **Don't mix configuration with business logic**
3. **Don't use mutable configuration** without understanding implications
4. **Don't ignore validation errors**

---

## 🧪 Testing Configuration

### Unit Test Example

```csharp
[Fact]
public void Subscriber_ShouldLoadConfiguration()
{
    // Arrange
    var settings = Options.Create(new RabbitMqSettings
    {
        HostName = "test-host",
        Port = 5672,
        Subscribers = new SubscriberSettings
        {
            Shipping = new SubscriberConfig
            {
                QueueName = "test_queue",
                RoutingKey = "test.route"
            }
        }
    });
    
    // Act
    var subscriber = new ShippingSubscriber(
        connectionFactory,
        settings,  // ← Mock configuration
        logger);
    
    // Assert
    Assert.Equal("test_queue", subscriber.ConfigurationKey);
}
```

---

## 🔍 Validation (Optional)

### Add Validation to Configuration

```csharp
// In Program.cs
builder.Services.AddOptions<RabbitMqSettings>()
    .Bind(builder.Configuration.GetSection("RabbitMq"))
    .ValidateDataAnnotations()  // ← Enable data annotation validation
    .ValidateOnStart();         // ← Validate on startup

// In RabbitMqSettings.cs
public class RabbitMqSettings
{
    [Required]
    [MinLength(1)]
    public string HostName { get; set; } = null!;
    
    [Range(1, 65535)]
    public int Port { get; set; }
    
    [Required]
    public string ExchangeName { get; set; } = null!;
}
```

---

## 📚 Additional Resources

- [Options Pattern in .NET](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options)
- [Configuration in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [IOptions vs IOptionsSnapshot vs IOptionsMonitor](https://andrewlock.net/choosing-the-right-ioptions-interface/)

---

## 🎯 Summary

The Options Pattern in OrderFlow.Core:

1. **Defines** strongly-typed configuration classes
2. **Binds** JSON configuration to C# objects
3. **Injects** configuration via `IOptions<T>`
4. **Accesses** configuration in a type-safe manner
5. **Validates** configuration on startup
6. **Supports** environment-specific overrides

This provides a clean, maintainable, and testable way to manage application configuration.

---

<div align="center">

**🎯 Type-Safe Configuration with Options Pattern**

*Clean, Maintainable, and Testable Configuration Management*

</div>
