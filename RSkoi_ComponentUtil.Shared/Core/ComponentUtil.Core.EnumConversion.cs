using System;
using System.Globalization;

namespace RSkoi_ComponentUtil.Core
{
    internal static class EnumConversion
    {
        /// <param name="value">enum value or an integral number</param>
        internal static ulong ToBits(object value)
        {
            var type = value.GetType();
            if (type.IsEnum)
                type = Enum.GetUnderlyingType(type);

            return Type.GetTypeCode(type) switch
            {
                TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64
                    => Convert.ToUInt64(value, CultureInfo.InvariantCulture),
                _   => unchecked((ulong)Convert.ToInt64(value, CultureInfo.InvariantCulture)),
            };
        }

        /// <param name="s">signed or unsigned integer string</param>
        internal static ulong ParseBits(string s)
        {
            s = s.Trim();
            return s.StartsWith("-")
                ? unchecked((ulong)long.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture))
                : ulong.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Bits that don't fit underlying type are cut off, e.g. -1 int becomes 0xFFFFFFFF for uint enum.
        /// </summary>
        internal static object ToEnum(Type enumType, ulong bits)
            => Enum.ToObject(enumType, FromBits(Enum.GetUnderlyingType(enumType), bits));

        /// <param name="value">enum value or an integral number</param>
        internal static object ToEnum(Type enumType, object value)
            => ToEnum(enumType, ToBits(value));

        /// <summary>
        /// Encodes an enum value.
        /// </summary>
        internal static object Encode(object value)
        {
            var bits = ToBits(value);
            if (!Is64Bit(value.GetType()))
                return unchecked((int)bits);

            var signed = unchecked((long)bits);
            return signed >= int.MinValue && signed <= int.MaxValue ? (object)(int)signed : signed;
        }

        static bool Is64Bit(Type type)
        {
            if (type.IsEnum)
                type = Enum.GetUnderlyingType(type);
            var typeCode = Type.GetTypeCode(type);
            return typeCode == TypeCode.Int64 || typeCode == TypeCode.UInt64;
        }

        static object FromBits(Type integralType, ulong bits) => Type.GetTypeCode(integralType) switch
        {
            TypeCode.SByte  => unchecked((sbyte)bits),
            TypeCode.Byte   => unchecked((byte)bits),
            TypeCode.Int16  => unchecked((short)bits),
            TypeCode.UInt16 => unchecked((ushort)bits),
            TypeCode.Int32  => unchecked((int)bits),
            TypeCode.UInt32 => unchecked((uint)bits),
            TypeCode.Int64  => unchecked((long)bits),
            _               => bits,
        };
    }
}
