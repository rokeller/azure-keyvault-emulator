using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Controllers;

namespace AzureKeyVaultEmulator.Converters;

internal sealed class KeyBundleConverter : JsonConverter<KeyBundle>
{
    public override KeyBundle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    public override void Write(Utf8JsonWriter writer, KeyBundle value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        JsonWebKey? key = value.Key;
        if (key != default)
        {
            writer.WritePropertyName("key");
            JsonSerializer.Serialize(writer, key, options);
        }

        if (null != value.Attributes)
        {
            writer.WritePropertyName("attributes");
            JsonSerializer.Serialize(writer, value.Attributes, options);
        }

        if (null != value.Tags)
        {
            writer.WritePropertyName("tags");
            JsonSerializer.Serialize(writer, value.Tags, options);
        }

        if (null != value.Managed)
        {
            writer.WritePropertyName("managed");
            JsonSerializer.Serialize(writer, value.Managed, options);
        }

        if (null != value.Release_policy)
        {
            writer.WritePropertyName("release_policy");
            JsonSerializer.Serialize(writer, value.Release_policy, options);
        }

        writer.WriteEndObject();
    }
}

