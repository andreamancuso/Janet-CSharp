namespace JanetSharp;

/// <summary>
/// Converts between Janet values and common .NET types.
/// </summary>
public static class JanetConvert
{
    /// <summary>
    /// Converts a .NET object to a Janet value.
    /// </summary>
    public static Janet ToJanet(object? value) => value switch
    {
        null => Janet.Nil,
        Janet j => j,
        double d => Janet.From(d),
        int i => Janet.From(i),
        float f => Janet.From((double)f),
        long l => Janet.From((double)l),
        bool b => Janet.From(b),
        string s => Janet.From(s),
        _ => throw new ArgumentException($"Cannot convert {value.GetType().Name} to Janet.", nameof(value))
    };

    /// <summary>
    /// Converts a Janet value to the specified .NET type.
    /// </summary>
    public static T ToClr<T>(Janet value) => (T)ToClr(value, typeof(T))!;

    /// <summary>
    /// Converts a Janet value to the specified .NET type.
    /// </summary>
    public static object? ToClr(Janet value, Type targetType)
    {
        if (value.IsNil)
        {
            if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                throw new InvalidOperationException($"Cannot convert Janet nil to non-nullable {targetType.Name}.");
            return null;
        }

        if (targetType == typeof(double))
            return value.AsNumber();
        if (targetType == typeof(int))
            return value.AsInteger();
        if (targetType == typeof(float))
            return (float)value.AsNumber();
        if (targetType == typeof(long))
            return (long)value.AsNumber();
        if (targetType == typeof(bool))
            return value.AsBoolean();
        if (targetType == typeof(string))
            return value.AsString();
        if (targetType == typeof(Janet))
            return value;

        throw new ArgumentException($"Cannot convert Janet {value.Type} to {targetType.Name}.", nameof(targetType));
    }
}
