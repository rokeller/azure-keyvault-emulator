using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AzureKeyVaultEmulator;

internal sealed class Store<T>
{
    private readonly DirectoryInfo root;
    private readonly JsonSerializerOptions writeOptions;
    private readonly JsonSerializerOptions readOptions;

    public Store(
        DirectoryInfo root,
        JsonSerializerOptions writeOptions = null,
        JsonSerializerOptions readOptions = null)
    {
        this.root = root;
        this.writeOptions = writeOptions;
        this.readOptions = readOptions;

        if (!root.Exists)
        {
            root.Create();
        }
    }

    public IEnumerable<KeyValuePair<string, T>> ReadObjects()
    {
        foreach (FileInfo file in root.EnumerateFiles("*.json"))
        {
            string key = ExtractObjectKeyFromFile(file);
            T obj = ReadObject(file);

            yield return new(key, obj);
        }
    }

    public void StoreObject(string key, T obj)
    {
        FileInfo file = GetFileForObjectKey(key);
        using FileStream fs = File.Open(file.FullName, FileMode.Create);
        using Utf8JsonWriter utf8JsonWriter = new(fs, new JsonWriterOptions
        {
#if DEBUG
            Indented = false,
#endif
        });
        JsonSerializer.Serialize(utf8JsonWriter, obj, writeOptions);

        // Flush the writer to ensure data is written to the file
        utf8JsonWriter.Flush();
    }

    private T ReadObject(FileInfo file)
    {
        using FileStream fs = File.Open(file.FullName, FileMode.Open);
        return JsonSerializer.Deserialize<T>(fs, readOptions);
    }

    private FileInfo GetFileForObjectKey(string key)
    {
        FileInfo file = new(Path.Combine(root.FullName, key + ".json"));
        return file;
    }

    private static string ExtractObjectKeyFromFile(FileInfo file)
    {
        return Path.GetFileNameWithoutExtension(file.Name);
    }
}
