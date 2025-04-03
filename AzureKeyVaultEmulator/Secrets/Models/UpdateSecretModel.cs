using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Secrets.Models;

public readonly struct UpdateSecretModel
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; init; }

    [JsonPropertyName("attributes")]
    public SecretAttributesModel Attributes { get; init; }

    [JsonPropertyName("tags")]
    public object Tags { get; init; }
}
