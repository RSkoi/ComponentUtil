using System;
using UnityEngine;

using static RSkoi_ComponentUtil.ComponentUtil;
using static RSkoi_ComponentUtil.ComponentUtil.PropertyTrackerData;

namespace RSkoi_ComponentUtil.Core
{
    /// <summary>
    /// Converts between member values and encoded form for the tracker<br />
    /// - enums: int<br />
    /// - vectors, colors: '#'-separated string (see VectorConversion, ColorConversion)<br />
    /// - everything else: the value itself<br />
    /// WARNING: saved scenes depend on this format, do not change unless backwards compatible
    /// </summary>
    internal static class ValueConversion
    {
        /// <returns>encoded value, null if the value is null or if its vector type is unsupported</returns>
        internal static object Encode(object value, PropertyTrackerDataOptions flags)
        {
            if (value == null)
                return null;
            if (HasPropertyFlag(flags, PropertyTrackerDataOptions.IsInt))
                return Convert.ToInt32(value);
            if (HasPropertyFlag(flags, PropertyTrackerDataOptions.IsVector))
                return VectorConversion.VectorToStringByType(value.GetType(), value);
            if (HasPropertyFlag(flags, PropertyTrackerDataOptions.IsColor))
                return ColorConversion.ColorToString((Color)value);
            return value;
        }

        /// <summary>
        /// Encodes a value as a string (InvariantCulture) for Timeline and equal-comparisons.
        /// </summary>
        internal static string EncodeToString(object value, PropertyTrackerDataOptions flags)
            => InvariantConversion.ToInvariantString(Encode(value, flags));

        /// <summary>
        /// Converts an encoded value or encoded string back to a value of <paramref name="memberType"/>.
        /// This can throw exceptions.
        /// </summary>
        internal static object Decode(object encoded, Type memberType, PropertyTrackerDataOptions flags)
        {
            if (HasPropertyFlag(flags, PropertyTrackerDataOptions.IsInt))
            {
                if (memberType.IsInstanceOfType(encoded))
                    return encoded;
                var i = encoded is string s ? InvariantConversion.ParseInt(s) : Convert.ToInt32(encoded);
                // Enum.ToObject also handles enums that are not backed by int
                return memberType.IsEnum ? Enum.ToObject(memberType, i) : InvariantConversion.ChangeType(i, memberType);
            }

            if (encoded is string encodedString)
            {
                if (HasPropertyFlag(flags, PropertyTrackerDataOptions.IsVector))
                    return VectorConversion.FloatValuesToVectorByType(memberType, VectorConversion.StringToVectorValues(encodedString));
                if (HasPropertyFlag(flags, PropertyTrackerDataOptions.IsColor))
                    return ColorConversion.StringToColor(encodedString);
            }

            if (encoded == null || memberType.IsInstanceOfType(encoded))
                return encoded;
            return InvariantConversion.ChangeType(encoded, memberType);
        }
    }
}
