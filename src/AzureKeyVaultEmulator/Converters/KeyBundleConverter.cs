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
            writer.WriteStartObject();

            if (null != key.Kid)
            {
                writer.WriteString("kid", key.Kid);
            }

            if (key.Kty.HasValue)
            {
                writer.WritePropertyName("kty");
                JsonSerializer.Serialize(writer, key.Kty, options);
            }

            if (null != key.Key_ops)
            {
                writer.WritePropertyName("key_ops");
                JsonSerializer.Serialize(writer, key.Key_ops, options);
            }

            if (null != key.N)
            {
                writer.WriteString("n", key.N);
            }

            if (null != key.E)
            {
                writer.WriteString("e", key.E);
            }

            if (null != key.D)
            {
                writer.WriteString("d", key.D);
            }

            if (null != key.Dp)
            {
                writer.WriteString("dp", key.Dp);
            }

            if (null != key.Dq)
            {
                writer.WriteString("dq", key.Dq);
            }

            if (null != key.Qi)
            {
                writer.WriteString("qi", key.Qi);
            }

            if (null != key.P)
            {
                writer.WriteString("p", key.P);
            }

            if (null != key.Q)
            {
                writer.WriteString("q", key.Q);
            }

            if (null != key.K)
            {
                writer.WriteString("k", key.K);
            }

            if (key.Crv.HasValue)
            {
                writer.WritePropertyName("crv");
                JsonSerializer.Serialize(writer, key.Crv, options);
            }

            if (null != key.X)
            {
                writer.WriteString("x", key.X);
            }

            if (null != key.Y)
            {
                writer.WriteString("y", key.Y);
            }

            writer.WriteEndObject();
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

