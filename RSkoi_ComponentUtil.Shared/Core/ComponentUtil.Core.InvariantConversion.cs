using System;
using System.Globalization;

namespace RSkoi_ComponentUtil.Core
{
    internal static class InvariantConversion
    {
        internal static string ToInvariantString(object value)
            => Convert.ToString(value, CultureInfo.InvariantCulture);

        internal static float ParseFloat(string s)
            => float.Parse(NormalizeDecimalSeparator(s), NumberStyles.Float, CultureInfo.InvariantCulture);

        internal static int ParseInt(string s)
            => int.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture);

        /// <summary>
        /// This is a Convert.ChangeType with invariant culture, use for floats as it accepts both decimal comma and point
        /// </summary>
        internal static object ChangeType(object value, Type type)
        {
            if (value is string s && IsFloatingPoint(type))
                value = NormalizeDecimalSeparator(s);
            return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        internal static bool IsFloatingPoint(Type type)
            => type == typeof(float) || type == typeof(double) || type == typeof(decimal);

        // for backwards compatability
        static string NormalizeDecimalSeparator(string s) => s.Replace(',', '.');
    }
}
