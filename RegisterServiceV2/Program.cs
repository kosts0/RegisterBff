using Confluent.Kafka;
using Microsoft.Extensions.Options;
using RegisterServiceV2.OidConsumer.Options;
using RegisterServiceV2.OidConsumer.Services;
using StackExchange.Redis;


var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();
// Bind options
builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection("Kafka"));
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));
builder.Services.Configure<LockOptions>(builder.Configuration.GetSection("Locking"));



// Redis connection multiplexer (singleton)
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
    return ConnectionMultiplexer.Connect(opts.ConnectionString);
});


// Locking service
builder.Services.AddSingleton<IDistributedLock, RedisDistributedLock>();


// Message processor (your business logic lives here)
builder.Services.AddScoped<IMessageProcessor, MessageProcessor>();


// Kafka consumer background service
builder.Services.AddHostedService<KafkaConsumerService>();


var host = builder.Build();
await host.RunAsync();