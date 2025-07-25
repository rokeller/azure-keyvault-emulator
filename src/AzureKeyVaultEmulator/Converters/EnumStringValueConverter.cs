using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzureKeyVaultEmulator.Converters;

internal sealed class EnumStringValueConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsEnum;
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type generic = typeof(Converter<>);
        Type constructed = generic.MakeGenericType(typeToConvert);
        return (JsonConverter?)Activator.CreateInstance(constructed);
    }

    public static Converter<T> Create<T>() where T : struct, Enum
    {
        Type typeToConvert = typeof(T);
        Type generic = typeof(Converter<>);
        Type constructed = generic.MakeGenericType(typeToConvert);
        Converter<T>? converter = (Converter<T>?)Activator.CreateInstance(constructed);

        if (null == converter)
        {
            throw new NotSupportedException();
        }

        return converter;
    }

    internal sealed class Converter<T> : JsonConverter<T>, IEnumToStringConvertible<T>
        where T : struct, Enum
    {
        private readonly Dictionary<string, T> decode;
        private readonly Dictionary<T, string> encode;

        public Converter()
        {
            Type t = typeof(T);
            FieldInfo[] fields = t.GetFields(BindingFlags.Public | BindingFlags.Static);
            decode = new Dictionary<string, T>(fields.Length, StringComparer.OrdinalIgnoreCase);
            encode = new Dictionary<T, string>(fields.Length);

            foreach (FieldInfo field in fields)
            {
                EnumMemberAttribute? attr = field.GetCustomAttribute<EnumMemberAttribute>();
                string name;
                T value = Enum.Parse<T>(field.Name);
                if (null != attr && null != attr.Value)
                {
                    name = attr.Value!;
                }
                else
                {
                    name = field.Name;
                }

                decode.Add(name, value);
                encode.Add(value, name);
            }
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? stringKey = reader.GetString();
            if (null == stringKey)
            {
                return default;
            }
            else if (!decode.TryGetValue(stringKey, out T value))
            {
                return default;
            }
            else
            {
                return value;
            }
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (encode.TryGetValue(value, out string? name) && null != name)
            {
                writer.WriteStringValue(name);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public string ToString(T value)
        {
            if (encode.TryGetValue(value, out string? name) && null != name)
            {
                return name;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}

