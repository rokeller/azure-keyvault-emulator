using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Controllers;

#if KEYVAULT_API_7_4
using JsonWebKeyType = AzureKeyVaultEmulator.Controllers.JsonWebKeyKty;
using JsonWebKeyCurveName = AzureKeyVaultEmulator.Controllers.JsonWebKeyCrv;
#endif

namespace AzureKeyVaultEmulator.Converters;

internal sealed class JsonWebKeyConverter : JsonConverter<JsonWebKey>
{
    public override JsonWebKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        JsonWebKey res = new();
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    switch (reader.GetString())
                    {
                        case "kid":
                            res.Kid = JsonSerializer.Deserialize<string>(ref reader, options);
                            break;
                        case "kty":
                            res.Kty = JsonSerializer.Deserialize<JsonWebKeyType>(ref reader, options);
                            break;
                        case "key_ops":
                            res.Key_ops = JsonSerializer.Deserialize<List<string>>(ref reader, options);
                            break;
                        case "n":
                            res.N = JsonSerializer.Deserialize<string>(ref reader, options);
                            break;
                        case "e":
                            res.E = JsonSerializer.Deserialize<string>(ref reader, options);
                            break;
                        case "d":
                            res.D = JsonSerializer.Deserialize<string>(ref reader, options);
                            break;
                        case "dp":
                            res.Dp = JsonSerializer.Deserialize<string>(ref reader, options);
                            break;
                        case "dq":
                            res.Dq = JsonSerializer.Deserialize<string>(ref reader, options);
                            break;
                        case "qi":
                            res.Qi = JsonSerializer.Deserialize<string>(ref reader, options);
                            break;
                        case "p":
                            res.P = JsonSerializer.Deserialize<string>(ref reader, options);
                            break;
                        case "q":
                            res.Q = JsonSerializer.Deserialize<string>(ref reader, options);
                            break;
                        case "k":
                            res.K = JsonSerializer.Deserialize<string>(ref reader, options);
                            break;
                        case "key_hsm":
                            res.Key_hsm = JsonSerializer.Deserialize<string>(ref reader, options);
                            break;
                        case "crv":
                            res.Crv = JsonSerializer.Deserialize<JsonWebKeyCurveName>(ref reader, options);
                            break;
                        case "x":
                            res.X = JsonSerializer.Deserialize<string>(ref reader, options);
                            break;
                        case "y":
                            res.Y = JsonSerializer.Deserialize<string>(ref reader, options);
                            break;

                        default:
                            throw new NotSupportedException();
                    }
                    break;
                case JsonTokenType.EndObject:
                    return res;

                default:
                    throw new NotSupportedException();
            }
        }

        throw new NotSupportedException();
    }

    public override void Write(Utf8JsonWriter writer, JsonWebKey value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (null != value.Kid)
        {
            writer.WriteString("kid", value.Kid);
        }
        if (value.Kty.HasValue)
        {
            writer.WritePropertyName("kty");
            JsonSerializer.Serialize(writer, value.Kty, options);
        }
        if (null != value.Key_ops)
        {
            writer.WritePropertyName("key_ops");
            JsonSerializer.Serialize(writer, value.Key_ops, options);
        }
        if (null != value.N)
        {
            writer.WriteString("n", value.N);
        }
        if (null != value.E)
        {
            writer.WriteString("e", value.E);
        }
        if (null != value.D)
        {
            writer.WriteString("d", value.D);
        }
        if (null != value.Dp)
        {
            writer.WriteString("dp", value.Dp);
        }
        if (null != value.Dq)
        {
            writer.WriteString("dq", value.Dq);
        }
        if (null != value.Qi)
        {
            writer.WriteString("qi", value.Qi);
        }
        if (null != value.P)
        {
            writer.WriteString("p", value.P);
        }
        if (null != value.Q)
        {
            writer.WriteString("q", value.Q);
        }
        if (null != value.K)
        {
            writer.WriteString("k", value.K);
        }
        if (null != value.Key_hsm)
        {
            writer.WriteString("key_hsm", value.Key_hsm);
        }
        if (value.Crv.HasValue)
        {
            writer.WritePropertyName("crv");
            JsonSerializer.Serialize(writer, value.Crv, options);
        }
        if (null != value.X)
        {
            writer.WriteString("x", value.X);
        }
        if (null != value.Y)
        {
            writer.WriteString("y", value.Y);
        }

        writer.WriteEndObject();
    }
}
