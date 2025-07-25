# Azure KeyVault Emulator

This is work based originally on [Basis Theory](https://basistheory.com/)'s
[Azure KeyVault Emulator](https://github.com/Basis-Theory/azure-keyvault-emulator)
which is no longer maintained by them and was very limited in functionality -
so I try to address those things here.

The Azure KeyVault Emulator helps emulating interactions with Azure KeyVault
using the official Azure KeyVault clients/SDKs. It stores keys and secrets in
the file system so data is not lost on restarts.

## Running in container

For container images, see [azure-keyvault-emulator on ghcr.io ![GitHub Release](https://img.shields.io/github/v/release/rokeller/azure-keyvault-emulator)](https://github.com/rokeller/azure-keyvault-emulator/pkgs/container/azure-keyvault-emulator)

Don't forget to mount a volume to `/app/.vault` in the container to keep
persisted keys and secrets. The default path can be changed through the environment
variable `STORE__BASEDIR`. Please note, that keys and secrets stored in the local
file system are **not** protected/encrypted.

For added security, the emulator in the container image runs as non-root user
with ID `1654`
(see [_What's new in containers for .NET 8_](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8/containers#non-root-user)),
so please make sure you assign proper read/write/list permissions when mounting
a directory. For example:

```bash
sudo chown 1654 -R .vault
docker container run --rm -it -p 11001:11001 \
  -v $PWD/.vault:/app/.vault \
  ghcr.io/rokeller/azure-keyvault-emulator:v2.0.0-rc2
```

The container by default exposes only the HTTPS endpoints on port 11001.

## Supported Operations

### Keys

| Operation | EC | RSA | AES |
|---|---|---|---|
| Create Key | ✅ (P-256, P-384, P-512) / 🚫 (P-256K) | ✅ | ✅ |
| Delete Key | ✅ | ✅ | ✅ |
| Get Key (get latest) | ✅ | ✅ | ✅ |
| Get Key (by version) | ✅ | ✅ | ✅ |
| Get Keys | ✅ | ✅ | ✅ |
| Get Key Versions | ✅ | ✅ | ✅ |
| Update Key Metadata | ✅ | ✅ | ✅ |
| Import Key | ✅ | ✅ | ✅ |
| Release Key (Export Key) | 🚫 | 🚫 | 🚫 |
| Backup Key | ✅ <sup>*</sup> | ✅ <sup>*</sup> | ✅ <sup>*</sup> |
| Restore Key | ✅ <sup>*</sup> | ✅ <sup>*</sup> | ✅ <sup>*</sup> |
| Rotate Key | 🚫 | 🚫 | 🚫 |
| Get Key Rotation Policy | 🚫 | 🚫 | 🚫 |
| Update Key Rotation Policy | 🚫 | 🚫 | 🚫 |
| **Crypto Operations** |
| Encrypt / Decrypt | ⛔ | ✅ | 🚫 |
| Wrap / Unwrap | ⛔ | ✅  | 🚫 |
| Sign / Verify | ✅ (ES256, ES384, ES512) / 🚫 (ES256K) | ✅ (PS256, PS384, PS512, RS256, RS284, RS512) / 🚧 (RSNULL) | ⛔ |

| Key ||
|---|---|
| ✅ | Implemented in emulator / Supported by Azure Key Vault |
| 🚫 | Not Implemented in emulator, but supported by Azure Key Vault |
| ⛔ | Not Supported by Azure Key Vault |
| 🚧 | Reserved by Azure Key Vault, not available |

<sup>*</sup> The backup format used/produced by the emulator is **not** compatible
with the Azure Key Vault service's backup format and it is not encrypted. However,
keys backed up from the emulator can be restored with the emulator.

> **Note**: Deleted key APIs are not supported. Deletion of keys purges them immediately.

### Random Number Generation

✅ Random number generation between 1 and 128 bytes (`POST /rng`) is supported.

### Secrets

| | Operation |
|---|---|
| ✅ | Set Secret |
| ✅ | Get Secret (get latest) |
| ✅ | Get Secret (by version) |
| ✅ | Update Secret |
| ✅ | Delete Secret |
| ✅ | Get Secrets |
| ✅ | Get Secret Versions |
| ✅ <sup>*</sup> | Backup Secret |
| ✅ <sup>*</sup> | Restore Secret |

<sup>*</sup> The backup format used/produced by the emulator is **not** compatible
with the Azure Key Vault service's backup format. However, secrets backed up
from the emulator can be restored with the emulator.

> **Note**: Deleted secret APIs are not supported. Deletion of secrets purges them immediately.

## Requirements

### HTTPS

Azure KeyClient and SecretClient require HTTPS communication with a KeyVault
instance. When accessing the emulator on `localhost`, configure a trusted TLS
certificate with [dotnet dev-certs](https://docs.microsoft.com/en-us/dotnet/core/additional-tools/self-signed-certificates-guide#with-dotnet-dev-certs).
Alternatively, you can use the default self-signed certificate provided with the
emulator (see [.certs](./.certs)). This certificate is issued for the FQDN
`emulator.vault.azure.net` and has additional names in the SAN list:
`localhost.vault.azure.net`, `localhost`, as well as the IP address `127.0.0.1`.

### AuthN/AuthZ

Azure KeyClient and SecretClient use the
[ChallengeBasedAuthenticationPolicy](https://github.com/Azure/azure-sdk-for-net/blob/b30fa6d0d402511fdf3270c5d1d9ae5dfa2a0340/sdk/keyvault/Azure.Security.KeyVault.Shared/src/ChallengeBasedAuthenticationPolicy.cs#L64-L66)
to determine the authentication scheme used by the server. In order for the
KeyVault Emulator to work with the Azure SDK, the emulator requires JWT bearer
authentication in the `Authorization` header. The KeyVault Emulator only
validates that the JWT is well-formed, but does not validate issuer, audience,
token lifetime or any other such claims.

By default, you can use the following JWT token for authentication:

`eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNzM1Njg5NjAwLCJleHAiOjQxMDI0NDQ4MDAsImlzcyI6Imh0dHBzOi8vbG9jYWxob3N0LyJ9.42D_zJ3qM02NM_ExWU9S9jvNGMfpop3YuWT9lFqJ5yU`

For example:

```bash
curl -X 'GET' \
  'https://localhost.vault.azure.net:11001/secrets/foo' \
  -H 'accept: application/json' \
  -H 'Authorization:Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNzM1Njg5NjAwLCJleHAiOjQxMDI0NDQ4MDAsImlzcyI6Imh0dHBzOi8vbG9jYWxob3N0LyJ9.42D_zJ3qM02NM_ExWU9S9jvNGMfpop3YuWT9lFqJ5yU'
```

### Connect clients with default Azure credentials

Azure SDKs for different languages/runtimes typically come with an implementation
for default Azure credentials, see for example [DefaultAzureCredential Class for .Net](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet). Usually you don't want to use
different code paths for local development vs running in a production system.
The emulator supports this, but it does require you to specify the ID of the
Azure tenant when starting the emulator. This is because the Azure SDKs discover
the tenant ID for which to get a JWT from the challenge issued with the
`WWW-Authenticate` header.

You can achieve this by running:

```bash
AZURE_TENANTID=$(az account show | jq -r '.tenantId')
```

Then, pass the value of the `AZURE_TENANTID` variable to the emulator e.g. by
using the environment variable `AUTH__TENANTID` (please note the double underscore).

This requires `az` CLI. Of course, if instead you want to hardcode the tenant ID,
you can also do so on the command line, or in the [appsettings.json](src/AzureKeyVaultEmulator/appsettings.json).
The same goes for the path to the directory in which to persist keys and secrets.

## Development

All tests work against an in-memory [test server](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.testhost.testserver?view=aspnetcore-8.0).
Simply running `dotnet test` should give developers a good idea of the quality.
