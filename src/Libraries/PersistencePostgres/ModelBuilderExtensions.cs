using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValetaxTest.Core;

namespace ValetaxTest.PersistencePostgres;

public static class ModelBuilderExtensions
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public static PropertyBuilder<decimal> IsMoney(this PropertyBuilder<decimal> property) =>
        property.HasPrecision(Money.Precision, Money.Scale);

    /// <summary>Stores an enum by name, so the database is readable and does not depend on enum numbers.</summary>
    public static PropertyBuilder<TEnum> IsEnumString<TEnum>(this PropertyBuilder<TEnum> property, int maxLength = 32) =>
        property.HasConversion<string>().HasMaxLength(maxLength);

    /// <summary>Stores the property (usually a list of records) as JSON in a <c>jsonb</c> column.</summary>
    public static PropertyBuilder<TProperty> HasJsonConversion<TProperty>(this PropertyBuilder<TProperty> property)
        where TProperty : class
    {
        var comparer = new ValueComparer<TProperty>(
            (left, right) => JsonSerializer.Serialize(left, JsonOptions) == JsonSerializer.Serialize(right, JsonOptions),
            value => JsonSerializer.Serialize(value, JsonOptions).GetHashCode(StringComparison.Ordinal),
            value => JsonSerializer.Deserialize<TProperty>(JsonSerializer.Serialize(value, JsonOptions), JsonOptions)!);

        return property
            .HasConversion(
                value => JsonSerializer.Serialize(value, JsonOptions),
                json => JsonSerializer.Deserialize<TProperty>(json, JsonOptions)!,
                comparer)
            .HasColumnType("jsonb");
    }
}
