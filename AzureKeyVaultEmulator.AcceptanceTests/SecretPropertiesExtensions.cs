using Azure.Security.KeyVault.Secrets;

namespace AzureKeyVaultEmulator.AcceptanceTests;

internal static class SecretPropertiesExtensions
{
    public static SecretProperties WithTag(this SecretProperties props, string key, string value)
    {
        props.Tags[key] = value;

        return props;
    }
}
