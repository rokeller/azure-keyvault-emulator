using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AzureKeyVaultEmulator.Services;

/// <summary>
/// Defines the contract for a store of objects of <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">
/// The type of objects to store.
/// </typeparam>
public interface IStore<T>
{
    /// <summary>
    /// Lists all objects from the store.
    /// </summary>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> to use.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that results in a list of objects of
    /// <typeparamref name="T"/> found in the store.
    /// </returns>
    Task<List<T>> ListObjectsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Reads an object from the store asynchronously.
    /// </summary>
    /// <param name="key">
    /// The key of the object to read.
    /// </param>
    /// <param name="version">
    /// The version to read or null to read the latest version.
    /// </param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> to use.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that results in a reference or value
    /// of <typeparamref name="T"/> or null if the object was not found.
    /// </returns>
    Task<T?> ReadObjectAsync(string key, string? version, CancellationToken cancellationToken);

    /// <summary>
    /// Lists all versions of the object with the given key from the store.
    /// </summary>
    /// <param name="key">
    /// </param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> to use.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that results in a list of <typeparamref name="T"/>
    /// with one item per version, or null if the object does not exist in the store.
    /// </returns>
    Task<List<T>?> ListObjectVersionsAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Stores an object in the store asynchronously.
    /// </summary>
    /// <param name="key">
    /// The key under which to store the object.
    /// </param>
    /// <param name="version">
    /// The version to write or null to write the latest version.
    /// </param>
    /// <param name="obj">
    /// The <typeparamref name="T"/> object to store.
    /// </param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> to use.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that tracks completion of the operation.
    /// </returns>
    Task StoreObjectAsync(string key, string? version, T obj, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an object from the store asynchronously.
    /// </summary>
    /// <param name="key">
    /// The key of the object to delete.
    /// </param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> to use.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that tracks completion of the operation.
    /// </returns>
    Task DeleteObjectAsync(string key, CancellationToken cancellationToken);
}
