using Azure.Security.KeyVault.Keys;

namespace AzureKeyVaultEmulator.ApiTests;

public sealed class V74RngApisTests : RngApisTests
{
    public V74RngApisTests() : base(KeyClientOptions.ServiceVersion.V7_4)
    {
    }
}

public sealed class V75RngApisTests : RngApisTests
{
    public V75RngApisTests() : base(KeyClientOptions.ServiceVersion.V7_5)
    {
    }
}

public sealed class V76RngApisTests : RngApisTests
{
    public V76RngApisTests() : base(KeyClientOptions.ServiceVersion.V7_6)
    {
    }
}

public sealed class V20250701RngApisTests : RngApisTests
{
    public V20250701RngApisTests() : base(KeyClientOptions.ServiceVersion.V2025_07_01)
    {
    }
}
