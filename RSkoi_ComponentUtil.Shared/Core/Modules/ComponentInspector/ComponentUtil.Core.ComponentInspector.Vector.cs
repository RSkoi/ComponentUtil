using System;
using System.Linq;
using System.Globalization;
using UnityEngine;

using RSkoi_ComponentUtil.UI;
using RSkoi_ComponentUtil.Core;

namespace RSkoi_ComponentUtil
{
    internal static partial class ComponentUtilEditor
    {
        static bool ConfigVector(PropertyEntryContext ctx)
        {
            var valueString = VectorConversion.VectorToStringByType(ctx.Member.MemberType, ctx.Value);
            if (string.IsNullOrEmpty(valueString))
                return false;

            var uiEntry = ctx.UiEntry;
            var vectorEntry = new ComponentUtilUI.PropertyUIVectorEntry(uiEntry);
            vectorEntry.SetInteractable(ctx.Interactable);
            vectorEntry.RemoveAllInputEvents();
            // content type is set on the prefab
            vectorEntry.SetUIVectorValues(valueString);

            vectorEntry.RegisterInputEvents();
            vectorEntry.OnValueChanged += v => ApplyEdit(ctx, v);

            uiEntry.UiComponentSetValueResetDelegate = (v) =>
            {
                // suspending all (non-persistent) listeners because vectorEntry.SetUIVectorValues
                // will trigger OnValueChanged, which would add to tracker
                vectorEntry.RemoveAllInputEvents();
                vectorEntry.SetUIVectorValues((string)v);
                vectorEntry.RegisterInputEvents();
                return v;
            };

            return true;
        }

    }

    /// <summary>
    /// Provides methods to parse vectors to ComponentUtil format and back.
    /// </summary>
    public static class VectorConversion
    {
        /// <summary>
        /// Whether the type is one of the vector types ComponentUtil supports (Vector2, Vector3, Vector4, Quaternion).
        /// </summary>
        public static bool IsVectorType(Type t)
            => t == typeof(Vector2) || t == typeof(Vector3) || t == typeof(Vector4) || t == typeof(Quaternion);

        /// <summary>
        /// Converts an encoded vector string value to values in a float array<br/>
        /// Parsing to float can throw, exception handling is the responsibility of the user
        /// </summary>
        /// <param name="value">vector string value with # as separator</param>
        /// <returns>values of vector parsed to floats</returns>
        public static float[] StringToVectorValues(string value)
        {
            var split = value.Split('#');
            var count = split.Where(s => !string.IsNullOrEmpty(s)).Count();

            var i = 0;
            var vectorValues = new float[count];
            foreach (var s in split)
            {
                if (string.IsNullOrEmpty(s))
                    continue;

                // exceptions should be caught by user
                vectorValues[i] = InvariantConversion.ParseFloat(s);
                i++;
            }
            return vectorValues;
        }

        /// <summary>
        /// Converts vector values to new vector object.
        /// </summary>
        /// <param name="t">the vector type</param>
        /// <param name="values">the vector values</param>
        /// <returns>vector object or <b>null</b> (!) if provided type is not supported</returns>
        public static object FloatValuesToVectorByType(Type t, float[] values)
        {
            if (t == typeof(Vector2))
                return ValuesToVector2(values);
            else if (t == typeof(Vector3))
                return ValuesToVector3(values);
            else if (t == typeof(Vector4))
                return ValuesToVector4(values);
            else if (t == typeof(Quaternion))
                return ValuesToQuaternion(values);

            Logger.LogError($"FloatValuesToVectorByType was supplied not supported vector type {t.Name}");
            return null;
        }

        /// <summary>
        /// Converts a vector to its string format by its type.
        /// </summary>
        /// <param name="t">the vector type</param>
        /// <param name="v">the vector object</param>
        /// <returns>vector in string format or <b>null</b> (!) if provided type is not supported</returns>
        public static string VectorToStringByType(Type t, object v)
        {
            if (t == typeof(Vector2))
                return Vector2ToString((Vector2)v);
            else if (t == typeof(Vector3))
                return Vector3ToString((Vector3)v);
            else if(t == typeof(Vector4))
                return Vector4ToString((Vector4)v);
            else if(t == typeof(Quaternion))
                return QuaternionToString((Quaternion)v);

            Logger.LogError($"VectorToStringByType was supplied not supported vector type {t.Name}");
            return null;
        }

        public static string Vector2ToString(Vector2 v)
            => string.Format(CultureInfo.InvariantCulture, "##{0}#{1}", v.x, v.y);

        public static string Vector3ToString(Vector3 v)
            => string.Format(CultureInfo.InvariantCulture, "#{0}#{1}#{2}", v.x, v.y, v.z);

        public static string Vector4ToString(Vector4 v)
            => string.Format(CultureInfo.InvariantCulture, "{0}#{1}#{2}#{3}", v.x, v.y, v.z, v.w);

        public static string QuaternionToString(Quaternion v)
            => string.Format(CultureInfo.InvariantCulture, "{0}#{1}#{2}#{3}", v.x, v.y, v.z, v.w);

        public static Vector2 ValuesToVector2(float[] values)
            => new(values[0], values[1]);

        public static Vector3 ValuesToVector3(float[] values)
            => new(values[0], values[1], values[2]);

        public static Vector4 ValuesToVector4(float[] values)
            => new(values[0], values[1], values[2], values[3]);

        public static Quaternion ValuesToQuaternion(float[] values)
            => new(values[0], values[1], values[2], values[3]);
    }
}
