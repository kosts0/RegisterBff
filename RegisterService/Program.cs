using Confluent.Kafka;
using WorkerService1;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost";
    options.InstanceName = "local";
});
builder.Services.AddSingleton<IConsumer<string, string>>(sp =>
{
    var kafkaConsumerConfig = new ConsumerConfig();
    
    sp.GetService<IConfiguration>().GetSection("Kafka:PersonUpdateConsumer").Bind(kafkaConsumerConfig);
    builder.Configuration.GetSection("Kafka:PersonUpdateConsumer").Bind(kafkaConsumerConfig);
    return new ConsumerBuilder<string, string>(kafkaConsumerConfig).Build();
});
builder.Services.AddHostedService<LocalQueueWorker>();
builder.Services.AddHostedService<KafkaConsumerService>();
var host = builder.Build();
host.Run();