using Azure.Security.KeyVault.Keys;

namespace AzureKeyVaultEmulator.ApiTests;

public sealed class V74KeysApisTests : KeysApisTests
{
    public V74KeysApisTests() : base(KeyClientOptions.ServiceVersion.V7_4)
    {
    }
}

public sealed class V75KeysApisTests : KeysApisTests
{
    public V75KeysApisTests() : base(KeyClientOptions.ServiceVersion.V7_5)
    {
    }
}

public sealed class V76KeysApisTests : KeysApisTests
{
    public V76KeysApisTests() : base(KeyClientOptions.ServiceVersion.V7_6)
    {
    }
}

public sealed class V20250701KeysApisTests : KeysApisTests
{
    public V20250701KeysApisTests() : base(KeyClientOptions.ServiceVersion.V2025_07_01)
    {
    }
}
