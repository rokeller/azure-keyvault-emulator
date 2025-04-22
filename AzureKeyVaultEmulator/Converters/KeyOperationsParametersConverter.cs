using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Controllers;

namespace AzureKeyVaultEmulator.Converters;

internal sealed class KeyOperationsParametersConverter : JsonConverter<KeyOperationsParameters>
{
    public override KeyOperationsParameters Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        KeyOperationsParameters res = new();
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
                        case "alg":
                            res.Alg = JsonSerializer.Deserialize<KeyOperationsParametersAlg>(ref reader, options);
                            break;
                        case "value":
                            res.Value = JsonSerializer.Deserialize<string>(ref reader, options)!;
                            break;
                        case "iv":
                            res.Iv = JsonSerializer.Deserialize<string>(ref reader, options);
                            break;
                        case "aad":
                            res.Aad = JsonSerializer.Deserialize<string>(ref reader, options);
                            break;
                        case "tag":
                            res.Tag = JsonSerializer.Deserialize<string>(ref reader, options);
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

    public override void Write(Utf8JsonWriter writer, KeyOperationsParameters value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

