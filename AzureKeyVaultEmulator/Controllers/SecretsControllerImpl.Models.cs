using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AzureKeyVaultEmulator.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace AzureKeyVaultEmulator.Controllers;

partial class SecretsControllerImpl
{
    private readonly record struct BackedUpSecretVersions(
        string BackupVersion,
        string Name,
        List<SecretBundle> Versions
    );
}
