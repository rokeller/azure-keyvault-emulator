using System.Collections.Generic;

namespace AzureKeyVaultEmulator.Controllers;

partial class KeysControllerImpl
{
    private readonly record struct BackedUpKeyVersions(
        string BackupVersion,
        string Name,
        List<KeyBundle> Versions
    );
}
