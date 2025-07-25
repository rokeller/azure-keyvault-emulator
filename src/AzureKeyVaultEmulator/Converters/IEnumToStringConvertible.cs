using System;

namespace AzureKeyVaultEmulator.Converters;

/// <summary>
/// Helper contract for enum value to string conversion.
/// </summary>
/// <typeparam name="T">
/// The type of the enum.
/// </typeparam>
internal interface IEnumToStringConvertible<T> where T : struct, Enum
{
    /// <summary>
    /// Converts the given <paramref name="value"/> to a string.
    /// </summary>
    /// <param name="value">
    /// The enum value to convert.
    /// </param>
    /// <returns>
    /// The string representation of the enum value.
    /// </returns>
    string ToString(T value);
}
