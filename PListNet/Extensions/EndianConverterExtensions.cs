using System.Buffers.Binary;

namespace PListNet.Extensions;

internal static class EndianConverterExtensions
{
    public static short ToInt16(this byte[] value, int startIndex = 0)
        => BinaryPrimitives.ReadInt16BigEndian(value.AsSpan(startIndex));

    public static int ToInt32(this byte[] value, int startIndex = 0)
        => BinaryPrimitives.ReadInt32BigEndian(value.AsSpan(startIndex));

    public static long ToInt64(this byte[] value, int startIndex = 0)
        => BinaryPrimitives.ReadInt64BigEndian(value.AsSpan(startIndex));

    // unsigned
    public static ushort ToUInt16(this byte[] value, int startIndex = 0)
        => BinaryPrimitives.ReadUInt16BigEndian(value.AsSpan(startIndex));

    public static uint ToUInt32(this byte[] value, int startIndex = 0)
        => BinaryPrimitives.ReadUInt32BigEndian(value.AsSpan(startIndex));

    public static ulong ToUInt64(this byte[] value, int startIndex = 0)
        => BinaryPrimitives.ReadUInt64BigEndian(value.AsSpan(startIndex));

    public static float ToSingle(this byte[] value, int startIndex = 0)
        => BitConverter.Int32BitsToSingle(value.ToInt32(startIndex));

    public static double ToDouble(this byte[] value, int startIndex = 0)
        => BitConverter.Int64BitsToDouble(value.ToInt64(startIndex));

    public static byte[] GetBytes(this short value)
        => [(byte)(value >> 8), (byte)value];

    public static byte[] GetBytes(this int value)
        => [(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value];

    public static byte[] GetBytes(this long value)
        => [
            (byte)(value >> 56), (byte)(value >> 48), (byte)(value >> 40), (byte)(value >> 32),
            (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value
        ];

    public static byte[] GetBytes(this ushort value)
        => ((int)value).GetBytes();

    public static byte[] GetBytes(this uint value)
        => ((long)value).GetBytes();

    public static byte[] GetBytes(this ulong value)
        => ((long)value).GetBytes();

    public static byte[] GetBytes(this float value)
        => BitConverter.SingleToInt32Bits(value).GetBytes();

    public static byte[] GetBytes(this double value)
        => BitConverter.DoubleToInt64Bits(value).GetBytes();
}