using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;

namespace AzureKeyVaultEmulator.AcceptanceTests.ApiTests;

internal static class KeyVaultExtensions
{
    public static CreateKeyOptions WithKeyOperation(this CreateKeyOptions opts, KeyOperation op)
    {
        opts.KeyOperations.Add(op);
        return opts;
    }

    public static KeyProperties WithTag(this KeyProperties props, string key, string value)
    {
        props.Tags[key] = value;

        return props;
    }

    public static SecretProperties WithTag(this SecretProperties props, string key, string value)
    {
        props.Tags[key] = value;

        return props;
    }
}
