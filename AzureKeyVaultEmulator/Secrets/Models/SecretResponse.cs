using System;
using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Secrets.Models;

public readonly struct SecretResponse
{
    [JsonPropertyName("id")]
    public Uri Id { get; init; }

    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Value { get; init; }

    [JsonPropertyName("attributes")]
    public SecretAttributesModel Attributes { get; init; }

    [JsonPropertyName("contentType")]
    public string ContentType { get; init; }

    [JsonPropertyName("tags")]
    public object Tags { get; init; }
}
