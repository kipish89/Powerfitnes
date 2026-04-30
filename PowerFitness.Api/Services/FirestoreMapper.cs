using System.Reflection;
using System.Text.Json;

namespace PowerFitness.Api.Services;

internal static class FirestoreMapper
{
    public static Dictionary<string, object?> ToFirestoreFields<T>(T value)
    {
        var fields = new Dictionary<string, object?>();
        foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.CanRead)
            {
                fields[property.Name] = ToFirestoreValue(property.GetValue(value));
            }
        }

        return fields;
    }

    public static object? ToFirestoreValue(object? value)
    {
        if (value is null)
        {
            return new Dictionary<string, object?> { ["nullValue"] = null };
        }

        var valueType = value.GetType();
        var underlying = Nullable.GetUnderlyingType(valueType) ?? valueType;

        if (underlying == typeof(string))
        {
            return new Dictionary<string, object?> { ["stringValue"] = value };
        }

        if (underlying == typeof(Guid))
        {
            return new Dictionary<string, object?> { ["stringValue"] = value.ToString() };
        }

        if (underlying == typeof(bool))
        {
            return new Dictionary<string, object?> { ["booleanValue"] = value };
        }

        if (underlying == typeof(int) || underlying == typeof(long))
        {
            return new Dictionary<string, object?> { ["integerValue"] = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) };
        }

        if (underlying == typeof(decimal) || underlying == typeof(double) || underlying == typeof(float))
        {
            return new Dictionary<string, object?> { ["doubleValue"] = Convert.ToDouble(value) };
        }

        if (underlying == typeof(DateTime))
        {
            return new Dictionary<string, object?> { ["timestampValue"] = ((DateTime)value).ToUniversalTime().ToString("O") };
        }

        throw new NotSupportedException($"Firestore mapping does not support type {underlying.Name}.");
    }

    public static T FromFirestoreDocument<T>(JsonElement document)
        where T : new()
    {
        var instance = new T();
        if (!document.TryGetProperty("fields", out var fields))
        {
            return instance;
        }

        foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanWrite || !fields.TryGetProperty(property.Name, out var firestoreValue))
            {
                continue;
            }

            property.SetValue(instance, FromFirestoreValue(firestoreValue, property.PropertyType));
        }

        return instance;
    }

    private static object? FromFirestoreValue(JsonElement firestoreValue, Type targetType)
    {
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (firestoreValue.TryGetProperty("nullValue", out _))
        {
            return null;
        }

        if (underlying == typeof(string) && firestoreValue.TryGetProperty("stringValue", out var stringValue))
        {
            return stringValue.GetString() ?? string.Empty;
        }

        if (underlying == typeof(Guid) && firestoreValue.TryGetProperty("stringValue", out var guidValue))
        {
            var text = guidValue.GetString();
            return string.IsNullOrWhiteSpace(text) ? Guid.Empty : Guid.Parse(text);
        }

        if (underlying == typeof(bool) && firestoreValue.TryGetProperty("booleanValue", out var boolValue))
        {
            return boolValue.GetBoolean();
        }

        if (underlying == typeof(int) && firestoreValue.TryGetProperty("integerValue", out var intValue))
        {
            return int.Parse(intValue.GetString() ?? "0", System.Globalization.CultureInfo.InvariantCulture);
        }

        if (underlying == typeof(long) && firestoreValue.TryGetProperty("integerValue", out var longValue))
        {
            return long.Parse(longValue.GetString() ?? "0", System.Globalization.CultureInfo.InvariantCulture);
        }

        if (underlying == typeof(decimal))
        {
            if (firestoreValue.TryGetProperty("doubleValue", out var decimalDoubleValue))
            {
                return Convert.ToDecimal(decimalDoubleValue.GetDouble());
            }

            if (firestoreValue.TryGetProperty("integerValue", out var decimalIntegerValue))
            {
                return decimal.Parse(decimalIntegerValue.GetString() ?? "0", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        if (underlying == typeof(double) && firestoreValue.TryGetProperty("doubleValue", out var doubleValue))
        {
            return doubleValue.GetDouble();
        }

        if (underlying == typeof(float) && firestoreValue.TryGetProperty("doubleValue", out var floatValue))
        {
            return (float)floatValue.GetDouble();
        }

        if (underlying == typeof(DateTime) && firestoreValue.TryGetProperty("timestampValue", out var timestampValue))
        {
            return DateTime.Parse(timestampValue.GetString() ?? DateTime.UtcNow.ToString("O"), null, System.Globalization.DateTimeStyles.RoundtripKind);
        }

        return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
    }
}
