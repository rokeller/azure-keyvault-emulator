using System.Collections.Generic;
using AzureKeyVaultEmulator.Secrets.Models;

namespace AzureKeyVaultEmulator.Secrets.Services;

public interface IKeyVaultSecretService
{
    IEnumerable<SecretResponse> Get(string name);
    SecretResponse? Get(string name, string version);
    SecretResponse SetSecret(string name, SetSecretModel requestBody);
    SecretResponse UpdateSecret(string name, string version, SecretResponse secret);
}
