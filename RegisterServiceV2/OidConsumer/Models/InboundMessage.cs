using System.Text.Json;
using System.Text.Json.Serialization;

namespace RegisterServiceV2.OidConsumer.Models;


public sealed class InboundMessage
{
    [JsonPropertyName("oid")] public required string Oid { get; init; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; init; }


    public string? Payload => Extra is null ? null : JsonSerializer.Serialize(Extra);


    public static bool TryParse(ReadOnlySpan<byte> data, out InboundMessage? msg)
    {
        try
        {
            msg = JsonSerializer.Deserialize<InboundMessage>(data);
            return msg is not null && !string.IsNullOrWhiteSpace(msg.Oid);
        }
        catch
        {
            msg = null;
            return false;
        }
    }
}