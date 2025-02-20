using System;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using AzureKeyVaultEmulator.Keys.Models;
using Microsoft.AspNetCore.WebUtilities;

namespace AzureKeyVaultEmulator.Keys.Services;

internal sealed class JsonWebKeyModelDeserializer : JsonConverter<JsonWebKeyModel>
{
    internal static JsonSerializerOptions AddConverter(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonWebKeyModelDeserializer());
        return options;
    }

    public override JsonWebKeyModel Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        JsonWebKeyModel model = JsonSerializer.Deserialize<JsonWebKeyModel>(ref reader);
        if (model.KeyType == "RSA")
        {
            RSAParameters rsaParameters = new()
            {
                D = WebEncoders.Base64UrlDecode(model.D),
                DP = WebEncoders.Base64UrlDecode(model.Dp),
                DQ = WebEncoders.Base64UrlDecode(model.Dq),
                Exponent = WebEncoders.Base64UrlDecode(model.E),
                Modulus = WebEncoders.Base64UrlDecode(model.N),
                P = WebEncoders.Base64UrlDecode(model.P),
                Q = WebEncoders.Base64UrlDecode(model.Q),
                InverseQ = WebEncoders.Base64UrlDecode(model.Qi),
            };
            RSA rsa = RSA.Create(rsaParameters);
            JsonWebKeyModel json = model;
            model = new(rsa)
            {
                KeyHsm = json.KeyHsm,
                KeyOperations = json.KeyOperations,
                KeyIdentifier = json.KeyIdentifier,
            };
        }

        return model;
    }

    public override void Write(Utf8JsonWriter writer, JsonWebKeyModel value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
