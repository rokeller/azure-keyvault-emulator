using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AzureKeyVaultEmulator.Services;

internal sealed class Store<T> : IStore<T>
{
    private readonly DirectoryInfo root;
    private readonly JsonSerializerOptions? writeOptions;
    private readonly JsonSerializerOptions? readOptions;

    public Store(
        DirectoryInfo root,
        JsonSerializerOptions? writeOptions = null,
        JsonSerializerOptions? readOptions = null)
    {
        this.root = root;
        this.writeOptions = writeOptions;
        this.readOptions = readOptions;

        if (!root.Exists)
        {
            root.Create();
        }
    }

    public Task<List<T>> ListObjectsAsync(CancellationToken cancellationToken)
    {
        FileInfo[] files = root.GetFiles("*.json", SearchOption.TopDirectoryOnly);
        return ReadObjectsAsync(files, cancellationToken)!;
    }

    public async Task<T?> ReadObjectAsync(
        string key,
        string? version,
        CancellationToken cancellationToken)
    {
        FileInfo file = GetFileForObject(key, version);
        if (!file.Exists)
        {
            return default;
        }

        using FileStream fs = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
        return await JsonSerializer
            .DeserializeAsync<T>(fs, readOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<List<T>?> ListObjectVersionsAsync(string key, CancellationToken cancellationToken)
    {
        DirectoryInfo versionsDir = GetDirForObjectVersions(key);
        if (!versionsDir.Exists)
        {
            return Task.FromResult<List<T>?>(null);
        }

        FileInfo[] files = versionsDir.GetFiles("*.json");
        return ReadObjectsAsync(files, cancellationToken);
    }

    public async Task StoreObjectAsync(
        string key,
        string? version,
        T obj,
        CancellationToken cancellationToken)
    {
        FileInfo file = GetFileForObject(key, version);
        file.Directory!.Create();
        using FileStream fs = File.Open(file.FullName, FileMode.Create);
        using Utf8JsonWriter utf8JsonWriter = new(fs, new JsonWriterOptions
        {
#if DEBUG
            Indented = false,
#endif
        });
        JsonSerializer.Serialize(utf8JsonWriter, obj, writeOptions);

        // Flush the writer to ensure data is written to the file
        await utf8JsonWriter.FlushAsync(CancellationToken.None)
            .ConfigureAwait(ConfigureAwaitOptions.None);
    }

    public Task DeleteObjectAsync(
        string key,
        CancellationToken cancellationToken)
    {
        FileInfo latest = GetFileForObject(key, null);
        DirectoryInfo versionsDir = GetDirForObjectVersions(key);

        latest.Delete();
        versionsDir.Delete(recursive: true);

        return Task.CompletedTask;
    }

    private async Task<T> ReadObjectAsync(FileInfo file, CancellationToken cancellationToken)
    {
        using FileStream fs = File.Open(file.FullName, FileMode.Open);
        // We don't allow storing null values, so the deserializer should never
        // return null.
        return (await JsonSerializer.DeserializeAsync<T>(fs, readOptions, cancellationToken))!;
    }

    private async Task<List<T>?> ReadObjectsAsync(FileInfo[] files, CancellationToken cancellationToken)
    {
        T[] result = new T[files.Length];
        await Parallel.ForAsync(0, files.Length, cancellationToken, async (index, cancellationToken) =>
        {
            result[index] = await ReadObjectAsync(files[index], cancellationToken);
        });

        return result.ToList();
    }

    private FileInfo GetFileForObject(string key, string? version)
    {
        FileInfo file;
        if (null == version)
        {
            file = new(Path.Combine(root.FullName, key + ".json"));
        }
        else
        {
            file = new(Path.Combine(root.FullName, key, version + ".json"));
        }

        return file;
    }

    private DirectoryInfo GetDirForObjectVersions(string key)
    {
        return new(Path.Combine(root.FullName, key));
    }
}
