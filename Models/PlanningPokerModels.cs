using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScrumPoker.Models;

public class Player
{
    [JsonPropertyName("userName")]
    public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("vote")]
    public string? Vote { get; set; }

    [JsonPropertyName("connectionId")]
    public string ConnectionId { get; set; } = string.Empty;

    [JsonIgnore]
    public bool HasVoted => !string.IsNullOrEmpty(Vote);
}

public class PokerSession
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("players")]
    public List<Player> Players { get; set; } = new();

    [JsonPropertyName("votesRevealed")]
    public bool VotesRevealed { get; set; }

    public static readonly string[] FibonacciValues = { "☕", "1", "2", "3", "5", "8", "13", "21", "34", "55", "89", "?" };
}

public class JoinResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("session")]
    public PokerSession? Session { get; set; }
}

public class CreateRoomResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("roomId")]
    public string? RoomId { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}

public class RoomJoinResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("roomExists")]
    public bool RoomExists { get; set; }
}

public class PlayerListConverter : JsonConverter<List<Player>>
{
    public override List<Player> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected StartArray token");
        }

        var list = new List<Player>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            var player = JsonSerializer.Deserialize<Player>(ref reader, options);
            if (player != null)
            {
                list.Add(player);
            }
        }

        return list;
    }

    public override void Write(Utf8JsonWriter writer, List<Player> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var player in value)
        {
            JsonSerializer.Serialize(writer, player, options);
        }
        writer.WriteEndArray();
    }
} 