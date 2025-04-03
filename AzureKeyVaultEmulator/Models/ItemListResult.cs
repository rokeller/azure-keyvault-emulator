using System.Collections.Generic;

namespace AzureKeyVaultEmulator.Models;

public readonly record struct ItemListResult<T>(
    ICollection<T> Value,
    string NextLink);
