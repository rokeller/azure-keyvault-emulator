using System.Collections.Generic;

namespace AzureKeyVaultEmulator.Controllers;

partial class SecretsControllerImpl
{
    private readonly record struct BackedUpSecretVersions(
        string BackupVersion,
        string Name,
        List<SecretBundle> Versions
    );
}
