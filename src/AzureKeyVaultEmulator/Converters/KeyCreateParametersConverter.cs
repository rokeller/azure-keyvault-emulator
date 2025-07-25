using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Controllers;

namespace AzureKeyVaultEmulator.Converters;

internal sealed class KeyCreateParametersConverter : JsonConverter<KeyCreateParameters>
{
    public override KeyCreateParameters Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        KeyCreateParameters res = new();
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
                        case "kty":
                            res.Kty = JsonSerializer.Deserialize<KeyCreateParametersKty>(ref reader, options);
                            break;
                        case "key_size":
                            res.Key_size = JsonSerializer.Deserialize<int>(ref reader, options);
                            break;
                        case "key_ops":
                            res.Key_ops = JsonSerializer.Deserialize<List<Key_ops>>(ref reader, options);
                            break;
                        case "crv":
                            res.Crv = JsonSerializer.Deserialize<KeyCreateParametersCrv>(ref reader, options);
                            break;
                        case "attributes":
                            res.Attributes = JsonSerializer.Deserialize<KeyAttributes>(ref reader, options);
                            break;
                        case "tags":
                            res.Tags = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options);
                            break;
                        case "public_exponent":
                            res.Public_exponent = JsonSerializer.Deserialize<int>(ref reader, options);
                            break;
                        case "release_policy":
                            res.Release_policy = JsonSerializer.Deserialize<KeyReleasePolicy>(ref reader, options);
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

    public override void Write(Utf8JsonWriter writer, KeyCreateParameters value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

