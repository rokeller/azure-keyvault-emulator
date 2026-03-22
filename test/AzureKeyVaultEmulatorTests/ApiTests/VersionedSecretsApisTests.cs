using Azure.Security.KeyVault.Secrets;

namespace AzureKeyVaultEmulator.ApiTests;

public sealed class V74SecretsApisTests : SecretsApisTests
{
    public V74SecretsApisTests() : base(SecretClientOptions.ServiceVersion.V7_4)
    {
    }
}

public sealed class V75SecretsApisTests : SecretsApisTests
{
    public V75SecretsApisTests() : base(SecretClientOptions.ServiceVersion.V7_5)
    {
    }
}

public sealed class V76SecretsApisTests : SecretsApisTests
{
    public V76SecretsApisTests() : base(SecretClientOptions.ServiceVersion.V7_6)
    {
    }
}

public sealed class V20250701SecretsApisTests : SecretsApisTests
{
    public V20250701SecretsApisTests() : base(SecretClientOptions.ServiceVersion.V2025_07_01)
    {
    }
}
