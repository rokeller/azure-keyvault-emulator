using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Keys.Models;

public sealed class KeyAttributesModel
{
    [JsonPropertyName("created")]
    public int Created { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("exp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Expiration { get; set; }

    [JsonPropertyName("nbf")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NotBefore { get; set; }

    [JsonPropertyName("recoverableDays")]
    public int RecoverableDays { get; set; }

    [JsonPropertyName("recoveryLevel")]
    public string RecoveryLevel { get; set; }

    [JsonPropertyName("updated")]
    public int Updated { get; set; }
}
