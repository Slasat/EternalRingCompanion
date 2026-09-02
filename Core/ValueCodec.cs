using System;
using System.Globalization;

namespace EternalRingCompanion.Core;

public enum FieldType
{
    Byte,
    Int16,
    Int32,
    Int64,
    Float,
    Double
}

/// <summary>Parse / format / size helpers for the primitive value types the game stores.</summary>
public static class ValueCodec
{
    public static int SizeOf(FieldType type) => type switch
    {
        FieldType.Byte => 1,
        FieldType.Int16 => 2,
        FieldType.Int32 => 4,
        FieldType.Int64 => 8,
        FieldType.Float => 4,
        FieldType.Double => 8,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static byte[] Parse(FieldType type, string text)
    {
        text = text.Trim();
        return type switch
        {
            FieldType.Byte => new[] { byte.Parse(text, CultureInfo.InvariantCulture) },
            FieldType.Int16 => BitConverter.GetBytes(short.Parse(text, CultureInfo.InvariantCulture)),
            FieldType.Int32 => BitConverter.GetBytes(int.Parse(text, CultureInfo.InvariantCulture)),
            FieldType.Int64 => BitConverter.GetBytes(long.Parse(text, CultureInfo.InvariantCulture)),
            FieldType.Float => BitConverter.GetBytes(float.Parse(text, CultureInfo.InvariantCulture)),
            FieldType.Double => BitConverter.GetBytes(double.Parse(text, CultureInfo.InvariantCulture)),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    public static string Format(FieldType type, byte[] data) => type switch
    {
        FieldType.Byte => data[0].ToString(CultureInfo.InvariantCulture),
        FieldType.Int16 => BitConverter.ToInt16(data, 0).ToString(CultureInfo.InvariantCulture),
        FieldType.Int32 => BitConverter.ToInt32(data, 0).ToString(CultureInfo.InvariantCulture),
        FieldType.Int64 => BitConverter.ToInt64(data, 0).ToString(CultureInfo.InvariantCulture),
        FieldType.Float => BitConverter.ToSingle(data, 0).ToString("0.######", CultureInfo.InvariantCulture),
        FieldType.Double => BitConverter.ToDouble(data, 0).ToString("0.######", CultureInfo.InvariantCulture),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static long ToInt64(FieldType type, byte[] data) => type switch
    {
        FieldType.Byte => data[0],
        FieldType.Int16 => BitConverter.ToInt16(data, 0),
        FieldType.Int32 => BitConverter.ToInt32(data, 0),
        FieldType.Int64 => BitConverter.ToInt64(data, 0),
        FieldType.Float => (long)BitConverter.ToSingle(data, 0),
        FieldType.Double => (long)BitConverter.ToDouble(data, 0),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
