using System.Text.Json;
using System.Text.Json.Serialization;

namespace KnightShift.Models;

/// <summary>
/// Root object for lsblk JSON output
/// </summary>
public class LsblkOutput
{
    public List<LsblkDevice> blockdevices { get; set; } = new();
}

/// <summary>
/// Represents a block device from lsblk output
/// </summary>
public class LsblkDevice
{
    public string name { get; set; } = "";
    public string? mountpoint { get; set; }
    public string? fstype { get; set; }
    public string? label { get; set; }

    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? size { get; set; }

    public long? sizebytes { get; set; }
    public string? type { get; set; }
    public List<LsblkDevice>? children { get; set; }
}

/// <summary>
/// Custom JSON converter to handle both string and number types for size field
/// </summary>
public class FlexibleStringConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.Number:
                return reader.GetInt64().ToString();
            case JsonTokenType.Null:
                return null;
            default:
                return reader.GetString();
        }
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
