using WorkerService1;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddScoped<IDatabase>(cfg =>
{
    IConnectionMultiplexer multiplexer = ConnectionMultiplexer.Connect($"{configsOptions.RedisUrl},password={secretsOptions.RedisPassword}");
    return multiplexer.GetDatabase();
});
var host = builder.Build();
host.Run();