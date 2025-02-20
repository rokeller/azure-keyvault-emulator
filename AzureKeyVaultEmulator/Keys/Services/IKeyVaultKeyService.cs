using AzureKeyVaultEmulator.Keys.Models;

namespace AzureKeyVaultEmulator.Keys.Services;

public interface IKeyVaultKeyService
{
    KeyResponse Get(string name);
    KeyResponse Get(string name, string version);
    KeyResponse CreateKey(string name, CreateKeyModel key);

    KeyOperationResult WrapKey(string name, string version, KeyOperationParameters request);
    KeyOperationResult UnwrapKey(string name, string version, KeyOperationParameters request);

    KeyOperationResult Encrypt(string name, string version, KeyOperationParameters keyOperationParameters);
    KeyOperationResult Decrypt(string name, string version, KeyOperationParameters keyOperationParameters);
}
