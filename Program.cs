using System.Text;
using OrderFlow.Core.Configuration;
using OrderFlow.Core.Infrastructure.RabbitMQ;
using OrderFlow.Core.Services.Subscribers;
using HealthChecks.UI.Client;
using HealthChecks.RabbitMQ; // Add this using directive
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

// Enable UTF-8 encoding for console output to display emojis/icons
Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure RabbitMQ settings. builder.Configuration.GetSection("RabbitMq") retrieves the RabbitMQ configuration section from all the configuration sources (appsettings.json, environment variables, etc.) and binds it to the RabbitMqSettings class.
builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMq"));

// Register RabbitMQ services
builder.Services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
builder.Services.AddScoped<IMessagePublisher, RabbitMqPublisher>();

// Add RabbitMQ subscribers
// AddHostedService is used to register background services that run alongside the main application.
// Register subscribers as hosted services. These will run in the background when the application starts.
// Each subscriber listens to specific order events and processes them accordingly.
builder.Services.AddHostedService<OrderProcessingSubscriber>();
builder.Services.AddHostedService<PaymentVerificationSubscriber>();
builder.Services.AddHostedService<ShippingSubscriber>();
builder.Services.AddHostedService<NotificationSubscriber>();

// Get RabbitMQ settings to build proper connection string
var rabbitMqSettings = builder.Configuration.GetSection("RabbitMq").Get<RabbitMqSettings>();
var rabbitConnectionString = $"amqp://{rabbitMqSettings.UserName}:{rabbitMqSettings.Password}@{rabbitMqSettings.HostName}:{rabbitMqSettings.Port}";

// Configure Health Checks
builder.Services.AddHealthChecks()
    .AddRabbitMQ(
        rabbitConnectionString: rabbitConnectionString,
        name: "rabbitmq",
        failureStatus: HealthStatus.Degraded);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
// Map health check endpoint to /health for services like Kubernetes or monitoring tools to check application health
app.MapHealthChecks("/health", new HealthCheckOptions()
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
