using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Secrets.Models
{
    public class SecretAttributesModel
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("exp")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Expiration { get; set; }

        [JsonPropertyName("nbf")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? NotBefore { get; set; }
    }
}
