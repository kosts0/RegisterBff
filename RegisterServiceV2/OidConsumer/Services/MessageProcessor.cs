using RegisterServiceV2.OidConsumer.Models;

namespace RegisterServiceV2.OidConsumer.Services;


public sealed class MessageProcessor : IMessageProcessor
{
    private readonly ILogger<MessageProcessor> _log;


    public MessageProcessor(ILogger<MessageProcessor> log)
    {
        _log = log;
    }


    public async Task ProcessAsync(InboundMessage msg, CancellationToken ct)
    {
// TODO: put your domain logic here
        _log.LogInformation("Processing OID {Oid} with payload length {Len}", msg.Oid, msg.Payload?.Length ?? 0);
        await Task.Delay(50, ct); // simulate some work
    }
}