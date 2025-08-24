using RegisterServiceV2.OidConsumer.Models;

namespace RegisterServiceV2.OidConsumer.Services;


public interface IMessageProcessor
{
    /// <summary>
    /// Business logic for a single message. Throw to signal failure (will be retried by your infra).
    /// </summary>
    Task ProcessAsync(InboundMessage msg, CancellationToken ct);
}