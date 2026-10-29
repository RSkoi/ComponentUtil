using System;
using UnityEngine;

using static RSkoi_ComponentUtil.PropertyTrackerData;
using static RSkoi_ComponentUtil.ComponentUtilTracker;

namespace RSkoi_ComponentUtil.Core
{
    /// <summary>
    /// Converts between member values and encoded form for the tracker<br/>
    /// - enums: int (long for 64 bit enum values, see EnumConversion)<br/>
    /// - vectors, colors: '#'-separated string (see VectorConversion, ColorConversion)<br/>
    /// - everything else: the value itself<br/>
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
                return EnumConversion.Encode(value);
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
            if (HasPropertyFlag(flags, PropertyTrackerDataOptions.IsInt) && memberType.IsEnum)
            {
                if (memberType.IsInstanceOfType(encoded))
                    return encoded;
                var bits = encoded is string s ? EnumConversion.ParseBits(s) : EnumConversion.ToBits(encoded);
                return EnumConversion.ToEnum(memberType, bits);
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

        /// <summary>
        /// Sets the value of a member, converting it to the member type according to flags first.
        /// </summary>
        /// <param name="value">encoded value or a string of the encoded value</param>
        internal static void SetValueWithFlags(Member member, object targetObject, object value, PropertyTrackerDataOptions flags)
        {
            if (!CanSetOn(member, targetObject))
                return;

            object decoded;
            try
            {
                decoded = Decode(value, member.MemberType, flags);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                Logger.LogError($"Tried to set value {value} on {targetObject}.{member.Name}");
                return;
            }
            SetDecodedValue(member, targetObject, decoded);
        }

        internal static void SetDecodedValue(Member member, object targetObject, object value)
        {
            if (!CanSetOn(member, targetObject))
                return;

            try
            {
                member.SetValue(targetObject, value);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                Logger.LogError($"Tried to set value {value} on {targetObject}.{member.Name}");
            }
        }

        static bool CanSetOn(Member member, object targetObject)
        {
            if (member == null || targetObject == null)
            {
                Logger.LogError("Setting a value was passed no member or no targetObject");
                return false;
            }
            // this shit is beyond cursed
            // this is probably a destroyed Unity object that's not actually been set to null yet
            return targetObject.ToString() != "null";
        }
    }
}
