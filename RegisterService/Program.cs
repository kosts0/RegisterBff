using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using WorkerService1;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost";
});
builder.Services.AddSingleton<IConsumer<string, string>>(sp =>
{
    var kafkaConsumerConfig = new ConsumerConfig();
    
    sp.GetService<IConfiguration>().GetSection("Kafka:PersonUpdateConsumer").Bind(kafkaConsumerConfig);
    builder.Configuration.GetSection("Kafka:PersonUpdateConsumer").Bind(kafkaConsumerConfig);
    return new ConsumerBuilder<string, string>(kafkaConsumerConfig).Build();
});
builder.Services.AddDbContext<PostgresDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));
//builder.Services.AddHostedService<LocalQueueWorker>();
builder.Services.AddHostedService<KafkaConsumerService>();
builder.Services.AddSingleton<LocalQueueWorker>();
var host = builder.Build();
host.Run();