using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AzureKeyVaultEmulator.Services;

internal sealed class Store<T> : IStore<T>
{
    private readonly ILogger<Store<T>> logger;
    private readonly DirectoryInfo root;
    private readonly JsonSerializerOptions? writeOptions;
    private readonly JsonSerializerOptions? readOptions;

    public Store(
        ILogger<Store<T>> logger,
        DirectoryInfo root,
        JsonSerializerOptions? writeOptions = null,
        JsonSerializerOptions? readOptions = null)
    {
        this.logger = logger;
        this.root = root;
        this.writeOptions = writeOptions;
        this.readOptions = readOptions;

        if (!root.Exists)
        {
            root.Create();
        }
        logger.LogInformation("Using stored {Type} objects from {Path}",
            typeof(T), root.FullName);
    }

    public Task<List<T>> ListObjectsAsync(CancellationToken cancellationToken)
    {
        FileInfo[] files = root.GetFiles("*.latest", SearchOption.TopDirectoryOnly);
        return ReadRedirectedObjectsAsync(files, cancellationToken)!;
    }

    public Task<bool> ObjectExistsAsync(string key, CancellationToken cancellationToken)
    {
        FileInfo latest = GetFileForObject(key, null);
        return Task.FromResult(latest.Exists);
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

        if (null == version)
        {
            return await ReadRedirectedObjectAsync(file, cancellationToken)
                .ConfigureAwait(ConfigureAwaitOptions.None);
        }
        else
        {
            using FileStream fs = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            return await JsonSerializer
                .DeserializeAsync<T>(fs, readOptions, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public Task<List<T>?> ListObjectVersionsAsync(
        string key,
        CancellationToken cancellationToken)
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
        string version,
        bool isLatestVersion,
        T obj,
        CancellationToken cancellationToken)
    {
        FileInfo file = GetFileForObject(key, version);
        file.Directory!.Create();
        {
            using FileStream fs = File.Open(file.FullName, FileMode.Create);
            using Utf8JsonWriter utf8JsonWriter = new(fs, new JsonWriterOptions
            {
#if DEBUG
                Indented = true,
#else
                Indented = false,
#endif
            });
            JsonSerializer.Serialize(utf8JsonWriter, obj, writeOptions);

            // Flush the writer to ensure data is written to the file
            await utf8JsonWriter.FlushAsync(CancellationToken.None)
                .ConfigureAwait(ConfigureAwaitOptions.None);
        }

        if (isLatestVersion)
        {
            // Update the 'latest' redirect to point to this version.
            await UpdateLatestAsync(key, version).ConfigureAwait(ConfigureAwaitOptions.None);
        }
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

    private async Task<T> ReadObjectAsync(
        FileInfo file,
        CancellationToken cancellationToken)
    {
        using FileStream fs = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        // We don't allow storing null values, so the deserializer should never
        // return null.
        return (await JsonSerializer.DeserializeAsync<T>(fs, readOptions, cancellationToken))!;
    }

    private async Task<T?> ReadRedirectedObjectAsync(
        FileInfo redirectingFile,
        CancellationToken cancellationToken)
    {
        using StreamReader reader = new(redirectingFile.FullName);
        string version = await reader.ReadToEndAsync(cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None);

        string key = Path.GetFileNameWithoutExtension(redirectingFile.Name);
        FileInfo versionFile = GetFileForObject(key, version);

        return await ReadObjectAsync(versionFile, cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None);
    }

    private async Task<List<T>?> ReadRedirectedObjectsAsync(
        FileInfo[] files,
        CancellationToken cancellationToken)
    {
        T[] result = new T[files.Length];

        async ValueTask ReadAsync(int index, CancellationToken cancellationToken)
        {
            T? item = await ReadRedirectedObjectAsync(files[index], cancellationToken)
                .ConfigureAwait(ConfigureAwaitOptions.None);
            result[index] = item!;
        }
        await Parallel.ForAsync(0, files.Length, cancellationToken, ReadAsync)
            .ConfigureAwait(ConfigureAwaitOptions.None);

        return result.ToList();
    }

    private async Task<List<T>?> ReadObjectsAsync(
        FileInfo[] files,
        CancellationToken cancellationToken)
    {
        T[] result = new T[files.Length];

        async ValueTask ReadAsync(int index, CancellationToken cancellationToken)
        {
            result[index] = await ReadObjectAsync(files[index], cancellationToken)
                .ConfigureAwait(ConfigureAwaitOptions.None);
        }
        await Parallel.ForAsync(0, files.Length, cancellationToken, ReadAsync)
            .ConfigureAwait(ConfigureAwaitOptions.None);

        return result.ToList();
    }

    private FileInfo GetFileForObject(string key, string? version)
    {
        FileInfo file;
        if (null == version)
        {
            file = new(Path.Combine(root.FullName, key + ".latest"));
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

    private async Task UpdateLatestAsync(
        string key,
        string version)
    {
        // Write the latest version into a temp file, then move the temp file.
        string tempFileName = Path.Combine(root.FullName, $"key.{Guid.NewGuid()}");
        FileInfo tempFile = new(tempFileName);
        {
            using FileStream fs = File.Open(tempFile.FullName, FileMode.Create);
            using StreamWriter writer = new(fs);

            await writer.WriteAsync(version).ConfigureAwait(ConfigureAwaitOptions.None);
            await writer.FlushAsync(default);
        }

        FileInfo targetFile = GetFileForObject(key, null);
        tempFile.MoveTo(targetFile.FullName, overwrite: true);
    }
}
