# Azure KeyVault Emulator

This is work based originally on [Basis Theory](https://basistheory.com/)'s
[Azure KeyVault Emulator](https://github.com/Basis-Theory/azure-keyvault-emulator)
which is no longer maintained by them - so I try to do that here.

The Azure KeyVault Emulator helps emulating interactions with Azure KeyVault
using the official Azure KeyVault clients/SDKs.

## Running in container

For container images, see [azure-keyvault-emulator on ghcr.io ![GitHub Release](https://img.shields.io/github/v/release/rokeller/azure-keyvault-emulator)](https://github.com/rokeller/azure-keyvault-emulator/pkgs/container/azure-keyvault-emulator)

Don't forget to mount a volume to `/app/.vault` in the container to keep
persisted keys and secrets.


## Supported Operations

### Keys

#### RSA

- Create Key
- Get Key
- Get Key by Version
- Encrypt
- Decrypt
- Wrap Key
- Unwrap Key

Supported [Algorithms](https://learn.microsoft.com/en-us/rest/api/keyvault/keys/decrypt/decrypt?view=rest-keyvault-keys-7.4&tabs=HTTP#jsonwebkeyencryptionalgorithm)
for Encrypt, Decrypt, Wrap Key and Unwrap Key operations:
  - `RSA1_5`
  - `RSA-OAEP`

### Secrets

- Set Secret (Create new Secret Version)
- Get Secret
- Get Secret by Version
- Get Secret Versions
- Update Secret Version

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

Azure KeyClient and SecretClient use a
[ChallengeBasedAuthenticationPolicy](https://github.com/Azure/azure-sdk-for-net/blob/b30fa6d0d402511fdf3270c5d1d9ae5dfa2a0340/sdk/keyvault/Azure.Security.KeyVault.Shared/src/ChallengeBasedAuthenticationPolicy.cs#L64-L66)
to determine the authentication scheme used by the server. In order for the
KeyVault Emulator to work with the Azure SDK, the emulator requires JWT bearer
authentication in the `Authorization` header. The KeyVault Emulator only
validates that the JWT is well-formed, but does not validate issuer, audience,
token lifetime or any other such claims.

By default, you can use the following JWT token for authentication:

`eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJleHAiOjE4OTAyMzkwMjIsImlzcyI6Imh0dHBzOi8vbG9jYWxob3N0OjUwMDEvIn0.bHLeGTRqjJrmIJbErE-1Azs724E5ibzvrIc-UQL6pws`

For example:

```bash
curl -X 'GET' \
  'https://localhost:11001/secrets/foo' \
  -H 'accept: application/json' \
  -H 'Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJleHAiOjE4OTAyMzkwMjIsImlzcyI6Imh0dHBzOi8vbG9jYWxob3N0OjUwMDEvIn0.bHLeGTRqjJrmIJbErE-1Azs724E5ibzvrIc-UQL6pws'
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
AUTH__TENANTID=$(az account show | jq -r '.tenantId') \
  STORE__BASEDIR=~/path/to/my/.vault \
  dotnet run --project AzureKeyVaultEmulator
```

This requires `az` CLI. Of course, if instead you want to hardcode the tenant ID,
you can also do so on the command line, or in the [appsettings.json](./AzureKeyVaultEmulator/appsettings.json).
The same goes for the path to the directory in which to persist keys and secrets.

## Adding to docker-compose

> **Important**: This section is outdated and will be revised soon.

For the Azure KeyVault Emulator to be accessible from other containers in the
same compose file, a new OpenSSL certificate has to be generated:

1. Replace `<emulator-hostname>` and run the following script to generate a new public/private keypair:

    ```
    openssl req \
    -x509 \
    -newkey rsa:4096 \
    -sha256 \
    -days 3560 \
    -nodes \
    -keyout <emulator-hostname>.key \
    -out <emulator-hostname>.crt \
    -subj '/CN=<emulator-hostname>' \
    -extensions san \
    -config <( \
      echo '[req]'; \
      echo 'distinguished_name=req'; \
      echo '[san]'; \
      echo 'subjectAltName=DNS.1:localhost,DNS.2:<emulator-hostname>,DNS.3:localhost.vault.azure.net,DNS.4:<emulator-hostname>.vault.azure.net')
    ```

1. Export a `.pks` formatted key using the public/private keypair generated in the previous step:

    ```
    openssl pkcs12 -export -out <emulator-hostname>.pfx \
    -inkey <emulator-hostname>.key \
    -in <emulator-hostname>.crt
    ```

1. Trust the certificate in the login keychain

    ```
    sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain <emulator-hostname>.crt
    ```

1. Add a service to docker-compose.yml for Azure KeyVault Emulator:

    ```
    services:
      ...
      azure-keyvault-emulator:
        image: ghcr.io/rokeller/azure-keyvault-emulator:<specific-version>
        hostname: <emulator-hostname>.vault.azure.net
        ports:
          - 5001:5001
          - 5000:5000
        volumes:
          - <path-to-certs>:/https
        environment:
          - ASPNETCORE_URLS=https://+:5001;http://+:5000
          - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/<emulator-hostname>.pfx
          - KeyVault__Name=<emulator-hostname>
    ```

1. Modify the client application's entrypoint to add the self-signed certificate to the truststore. Example using docker-compose.yml to override the entrypoint:

    ```
    services:
      my-awesome-keyvault-client:
        container_name: my-awesome-client
        build:
          context: .
        depends_on:
          - azure-keyvault-emulator
        entrypoint: sh -c "cp /https/<emulator-hostname>.crt /usr/local/share/ca-certificates/<emulator-hostname>.crt && update-ca-certificates && exec <original-entrypoint>"
        volumes:
          - <path-to-certs>:/https
        environment:
          - KeyVault__BaseUrl=https://<emulator-hostname>.vault.azure.net:5001/
    ```

1. (Optional) Azure KeyVault SDKs verify the challenge resource URL as of v4.4.0 (read more [here](https://devblogs.microsoft.com/azure-sdk/guidance-for-applications-using-the-key-vault-libraries/)). 
To satisfy the new challenge resource verification requirements, do one of the following:
   1. Use an emulator hostname that ends with `.vault.azure.net` (e.g. `localhost.vault.azure.net`). A new entry may need to be added to `/etc/hosts` to properly resolve DNS (i.e. `127.0.0.1 localhost.vault.azure.net`).
   1. Set `DisableChallengeResourceVerification` to true in your client options to disable verification.
```csharp
var client = new SecretClient(
    new Uri("https://localhost.vault.azure.net:11001/"), 
    new LocalTokenCredential(), 
    new SecretClientOptions
    {
        DisableChallengeResourceVerification = true
    });
```

## Development

> **Important**: This section is outdated and will be revised soon.

The provided scripts will check for all dependencies, start docker, build the solution, and run all tests.

### Dependencies
- [Docker](https://www.docker.com/products/docker-desktop)
- [Docker Compose](https://www.docker.com/products/docker-desktop)
- [.NET 8](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build the KeyVault emulator and run Tests

> **Important**: This section is outdated and will be revised soon.

Run the following command from the root of the project:

```sh
make verify
```
