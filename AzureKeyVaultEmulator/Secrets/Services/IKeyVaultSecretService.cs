using AzureKeyVaultEmulator.Secrets.Models;

namespace AzureKeyVaultEmulator.Secrets.Services;

public interface IKeyVaultSecretService
{
    SecretResponse Get(string name);
    SecretResponse Get(string name, string version);
    SecretResponse SetSecret(string name, SetSecretModel requestBody);
}
