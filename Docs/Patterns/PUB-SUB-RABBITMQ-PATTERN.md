# ?? Pub/Sub Pattern with RabbitMQ in OrderFlow.Core

## Overview

OrderFlow.Core implements the Publish/Subscribe (Pub/Sub) pattern using RabbitMQ's **Topic Exchange** to enable decoupled, event-driven communication between components.

---

## ?? Pattern Architecture

```
????????????????        ???????????????????        ????????????????????
?   Publisher  ????????>?  Topic Exchange ????????>?   Subscribers    ?
? (Controller) ?        ? order_exchange  ?        ?  (4 Subscribers) ?
????????????????        ???????????????????        ????????????????????
     Publishes                   ?                          ?
     Messages                    ?                          ?
     with Routing Key       Routes by Pattern         Consume from
                                  ?                    Bound Queues
                                  ?
                    ?????????????????????????????
                    ?             ?             ?
            order.created   order.shipped   order.*
```

### Key Components

| Component | Role | Implementation |
|-----------|------|----------------|
| **Publisher** | Sends events | `RabbitMqPublisher` |
| **Exchange** | Routes messages | `order_exchange` (topic) |
| **Subscribers** | Process events | 4 Background Services |
| **Queues** | Store messages | One per subscriber |
| **Routing Keys** | Message routing | Pattern matching (e.g., `order.*`) |

---

## ?? Implementation

### 1. **Configure Exchange and Queues**

**File**: `appsettings.Development.json`

```json
{
  "RabbitMq": {
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

### 2. **Publisher - Sending Events**

**File**: `Infrastructure/RabbitMQ/RabbitMqPublisher.cs`

```csharp
public class RabbitMqPublisher : IMessagePublisher
{
    public void PublishOrderEvent(OrderEvent orderEvent)
    {
        var message = JsonSerializer.Serialize(orderEvent);
        var body = Encoding.UTF8.GetBytes(message);

        // Publish to exchange with routing key
        _channel.BasicPublish(
            exchange: _settings.ExchangeName,     // "order_exchange"
            routingKey: orderEvent.EventType,     // "order.created"
            basicProperties: properties,
            body: body);
    }
}
```

**Usage in Controller**:

```csharp
[HttpPost]
public IActionResult CreateOrder([FromBody] CreateOrderRequest request)
{
    var orderEvent = new OrderEvent
    {
        EventType = OrderEventTypes.OrderCreated,  // "order.created"
        OrderData = orderData
    };
    
    _messagePublisher.PublishOrderEvent(orderEvent);  // Publish!
    return Ok();
}
```

---

### 3. **Subscribers - Consuming Events**

**Base Class**: `RabbitMqSubscriberBase.cs`

```csharp
public abstract class RabbitMqSubscriberBase : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 1. Declare exchange
        _channel.ExchangeDeclare(exchange: Settings.ExchangeName, type: "topic");
        
        // 2. Declare queue
        _channel.QueueDeclare(queue: _subscriberConfig.QueueName, durable: true);
        
        // 3. Bind queue to exchange with routing key
        _channel.QueueBind(
            queue: _subscriberConfig.QueueName,
            exchange: Settings.ExchangeName,
            routingKey: _subscriberConfig.RoutingKey);  // Pattern matching!
        
        // 4. Start consuming
        _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
    }
}
```

**Concrete Subscriber**:

```csharp
public class OrderProcessingSubscriber : RabbitMqSubscriberBase
{
    protected override string ConfigurationKey => "OrderProcessing";
    
    protected override async Task ProcessMessageAsync(OrderEvent orderEvent)
    {
        // Business logic here
        Logger.LogInformation("Processing order: {OrderId}", orderEvent.OrderId);
    }
}
```

---

## ?? Routing Patterns

### Topic Exchange Routing

| Subscriber | Routing Key Pattern | Matches |
|------------|---------------------|---------|
| **OrderProcessing** | `order.created` | Exact: `order.created` |
| **Notification** | `order.*` | All: `order.created`, `order.shipped`, etc. |
| **PaymentVerification** | `payment.*` | All: `payment.verify`, `payment.refund`, etc. |
| **Shipping** | `order.shipped` | Exact: `order.shipped` |

### Pattern Matching Rules

- `*` (star) - matches exactly one word
- `#` (hash) - matches zero or more words
- Exact match - matches literal string

**Examples**:
```
order.created     ? Matches: order.created
order.*           ? Matches: order.created, order.shipped, order.cancelled
order.#           ? Matches: order.created, order.created.v2, order.x.y.z
```

---

## ?? Message Flow

```
???????????????????????????????????????????????????????????????????
? 1. Client Request                                                ?
?    POST /api/orders                                              ?
???????????????????????????????????????????????????????????????????
                  ?
                  ?
???????????????????????????????????????????????????????????????????
? 2. Controller Creates Event                                      ?
?    OrderEvent { EventType: "order.created", OrderData: {...} }  ?
???????????????????????????????????????????????????????????????????
                  ?
                  ?
???????????????????????????????????????????????????????????????????
? 3. Publisher Sends to Exchange                                   ?
?    BasicPublish(exchange: "order_exchange",                      ?
?                 routingKey: "order.created", ...)                ?
???????????????????????????????????????????????????????????????????
                  ?
                  ?
???????????????????????????????????????????????????????????????????
? 4. Exchange Routes to Queues (Based on Routing Key)             ?
?    ??> order_processing_queue (matches "order.created")         ?
?    ??> notification_queue (matches "order.*")                   ?
???????????????????????????????????????????????????????????????????
                  ?
                  ?
???????????????????????????????????????????????????????????????????
? 5. Subscribers Process Messages                                  ?
?    ??> OrderProcessingSubscriber ? Processes order              ?
?    ??> NotificationSubscriber ? Sends notification              ?
???????????????????????????????????????????????????????????????????
```

---

## ?? Subscribers Overview

### 1. OrderProcessingSubscriber
- **Routing Key**: `order.created`
- **Purpose**: Initial order processing (validation, inventory check)
- **Queue**: `order_processing_queue`

### 2. NotificationSubscriber
- **Routing Key**: `order.*` (wildcard - receives ALL order events)
- **Purpose**: Send notifications (email, SMS, push)
- **Queue**: `notification_queue`

### 3. PaymentVerificationSubscriber
- **Routing Key**: `payment.*`
- **Purpose**: Payment processing and verification
- **Queue**: `payment_verification_queue`

### 4. ShippingSubscriber
- **Routing Key**: `order.shipped`
- **Purpose**: Handle shipping operations
- **Queue**: `shipping_queue`

---

## ? Benefits

| Benefit | Description |
|---------|-------------|
| **Decoupling** | Publisher doesn't know about subscribers |
| **Scalability** | Add subscribers without changing publisher |
| **Flexibility** | Route messages based on patterns |
| **Reliability** | Messages stored in queues if subscriber down |
| **Parallel Processing** | Multiple subscribers process independently |
| **Single Responsibility** | Each subscriber handles specific concern |

---

## ?? Key Features

### 1. Durable Messages
```csharp
// Messages persist across RabbitMQ restarts
var properties = _channel.CreateBasicProperties();
properties.Persistent = true;
```

### 2. Manual Acknowledgment
```csharp
// Only remove message after successful processing
await ProcessMessageAsync(orderEvent);
_channel.BasicAck(deliveryTag: deliveryTag, multiple: false);
```

### 3. Quality of Service (QoS)
```csharp
// Process one message at a time
_channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
```

### 4. Error Handling
```csharp
// Requeue on processing error, discard on bad data
catch (JsonException)
{
    _channel.BasicNack(deliveryTag, multiple: false, requeue: false);
}
catch (Exception)
{
    _channel.BasicNack(deliveryTag, multiple: false, requeue: true);
}
```

---

## ?? Registration in DI

**File**: `Program.cs`

```csharp
// Publisher
builder.Services.AddScoped<IMessagePublisher, RabbitMqPublisher>();

// Subscribers (as Background Services)
builder.Services.AddHostedService<OrderProcessingSubscriber>();
builder.Services.AddHostedService<PaymentVerificationSubscriber>();
builder.Services.AddHostedService<ShippingSubscriber>();
builder.Services.AddHostedService<NotificationSubscriber>();
```

**Key Points**:
- Publisher is **Scoped** (created per HTTP request)
- Subscribers are **Background Services** (long-running, start with app)

---

## ?? Real-World Example

### Publishing an Order Event

```csharp
// 1. Create order event
var orderEvent = new OrderEvent
{
    OrderId = Guid.NewGuid(),
    EventType = OrderEventTypes.OrderCreated,  // "order.created"
    OrderData = new OrderData
    {
        CustomerName = "John Doe",
        ProductName = "Laptop",
        Quantity = 1,
        TotalAmount = 999.99m
    }
};

// 2. Publish to exchange
_messagePublisher.PublishOrderEvent(orderEvent);

// 3. RabbitMQ routes to subscribers:
//    ? OrderProcessingSubscriber (matches "order.created")
//    ? NotificationSubscriber (matches "order.*")
```

---

## ?? Message Routing Examples

### Scenario 1: Order Created
```
Publisher sends: "order.created"
??> OrderProcessingSubscriber ? (exact match)
??> NotificationSubscriber ? (wildcard match)
```

### Scenario 2: Order Shipped
```
Publisher sends: "order.shipped"
??> ShippingSubscriber ? (exact match)
??> NotificationSubscriber ? (wildcard match)
```

### Scenario 3: Payment Verified
```
Publisher sends: "payment.verified"
??> PaymentVerificationSubscriber ? (wildcard match)
```

---

## ?? Summary

**Pub/Sub Pattern in OrderFlow.Core**:

1. **Publisher** sends events to **Topic Exchange** with routing key
2. **Exchange** routes messages to queues based on **pattern matching**
3. **Subscribers** consume from their queues and process independently
4. **Decoupled** components communicate via events
5. **Scalable** and **flexible** message routing

**Result**: Clean, maintainable, event-driven architecture where components don't directly depend on each other.

---

<div align="center">

**?? Event-Driven Architecture with RabbitMQ**

*Decoupled, Scalable, and Reliable Message Processing*

</div>
