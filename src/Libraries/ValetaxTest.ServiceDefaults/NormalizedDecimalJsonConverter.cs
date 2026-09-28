using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ValetaxTest.ServiceDefaults;

/// <summary>
/// Writes decimals without trailing zeros: the database stores amounts as numeric(24,8),
/// but a client should see <c>10</c>, not <c>10.00000000</c>.
/// </summary>
public sealed class NormalizedDecimalJsonConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String
            ? decimal.Parse(reader.GetString()!, NumberStyles.Number, CultureInfo.InvariantCulture)
            : reader.GetDecimal();

    // Dividing by 1 written with 28 zeros after the point removes trailing zeros and keeps the value.
    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value / 1.0000000000000000000000000000m);
}
