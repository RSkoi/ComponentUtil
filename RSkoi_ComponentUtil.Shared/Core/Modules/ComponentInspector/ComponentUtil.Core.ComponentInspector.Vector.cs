using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

using RSkoi_ComponentUtil.UI;

namespace RSkoi_ComponentUtil
{
    public partial class ComponentUtil
    {
        private bool ConfigVector(ComponentUtilUI.GenericUIListEntry parentUiEntry,
            GameObject entry,
            ComponentUtilUI.PropertyUIEntry uiEntry,
            object input,
            PropertyInfo p,
            FieldInfo f,
            Type type,
            bool setMethodIsPublic,
            bool isProperty,
            object value,
            bool objectMode)
        {
            string valueString = VectorConversion.VectorToStringByType(type, value);
            if (valueString.IsNullOrEmpty())
                return false;

            ComponentUtilUI.PropertyUIVectorEntry vectorEntry = new(uiEntry);
            vectorEntry.SetInteractable(setMethodIsPublic);
            vectorEntry.RemoveAllInputEvents();
            // this is already set on the prefab so this is redundant
            //vectorEntry.SetContentType(InputField.ContentType.DecimalNumber);
            vectorEntry.SetUIVectorValues(valueString);

            vectorEntry.RegisterInputEvents();

            void ValueChangedEvents(string v)
            {
                if (isProperty)
                {
                    string defaultValue = VectorConversion.VectorToStringByType(p.PropertyType, p.GetValue(input, null));
                    if (objectMode)
                    {
                        AddPropertyToTracker(_selectedObject, _selectedComponent.gameObject, _selectedComponent, _selectedReferencePropertyUiEntry.PropertyNameValue,
                            p.Name, defaultValue, PropertyTrackerData.PropertyTrackerDataOptions.IsProperty | PropertyTrackerData.PropertyTrackerDataOptions.IsVector);
                        _selectedReferencePropertyUiEntry.SetBgColorEdited();
                    }
                    else
                        AddPropertyToTracker(_selectedObject, _selectedComponent.gameObject, _selectedComponent, p.Name, defaultValue,
                            PropertyTrackerData.PropertyTrackerDataOptions.IsProperty | PropertyTrackerData.PropertyTrackerDataOptions.IsVector);
                    SetVectorPropertyValue(p, v, input);
                }
                else
                {
                    string defaultValue = VectorConversion.VectorToStringByType(f.FieldType, f.GetValue(input));
                    if (objectMode)
                    {
                        AddPropertyToTracker(_selectedObject, _selectedComponent.gameObject, _selectedComponent, _selectedReferencePropertyUiEntry.PropertyNameValue,
                            f.Name, defaultValue, PropertyTrackerData.PropertyTrackerDataOptions.IsVector);
                        _selectedReferencePropertyUiEntry.SetBgColorEdited();
                    }
                    else
                        AddPropertyToTracker(_selectedObject, _selectedComponent.gameObject, _selectedComponent, f.Name, defaultValue,
                            PropertyTrackerData.PropertyTrackerDataOptions.IsVector);
                    SetVectorFieldValue(f, v, input);
                }

                uiEntry.SetBgColorEdited();
                ComponentUtilUI.TraverseAndSetEditedParents();
            }
            vectorEntry.OnValueChanged += ValueChangedEvents;

            uiEntry.UiComponentSetValueResetDelegate = (v) =>
            {
                // suspending all (non-persistent) listeners because vectorEntry.SetUIVectorValues
                // will trigger OnValueChanged, which would add to tracker
                vectorEntry.RemoveAllInputEvents();
                vectorEntry.SetUIVectorValues((string)v);
                vectorEntry.RegisterInputEvents();
                return v;
            };
            uiEntry.ResetOverrideDelegate = (v) =>
            {
                if (isProperty)
                    SetVectorPropertyValue(p, (string)v, input);
                else
                    SetVectorFieldValue(f, (string)v, input);

                return v;
            };
            uiEntry.ParentUiEntry = parentUiEntry;

            return true;
        }

        #region internal setters
        internal void SetVectorPropertyValue(PropertyInfo p, string value, object input)
        {
            try
            {
                float[] values = VectorConversion.StringToVectorValues(value);
                object vector = VectorConversion.FloatValuesToVectorByType(p.PropertyType, values);
                p.SetValue(input, vector, null);
            }
            catch (Exception e) {
                _logger.LogError(e);
                _logger.LogError($"Tried to set value {value} on {input}.{p.Name}");
            }
        }

        internal void SetVectorFieldValue(FieldInfo f, string value, object input)
        {
            try
            {
                float[] values = VectorConversion.StringToVectorValues(value);
                object vector = VectorConversion.FloatValuesToVectorByType(f.FieldType, values);
                f.SetValue(input, vector);
            }
            catch (Exception e) {
                _logger.LogError(e);
                _logger.LogError($"Tried to set value {value} on {input}.{f.Name}");
            }
        }
        #endregion internal setters

        /// <summary>
        /// provides methods to parse vectors to ComponentUtil format and back
        /// </summary>
        public static class VectorConversion
        {
            /// <summary>
            /// tries to convert a vector string value to a typed new vector object
            /// </summary>
            /// <param name="t">the vector type</param>
            /// <param name="value">vector string value with # as separator</param>
            /// <param name="vector">the converted vector</param>
            /// <returns>true if conversion was successful</returns>
            public static bool TryStringToVectorByType(Type t, string value, out object vector)
            {
                float[] values;
                try { values = StringToVectorValues(value); }
                catch (Exception e)
                {
                    _logger.LogError(e);
                    vector = null;
                    return false;
                }

                vector = FloatValuesToVectorByType(t, values);
                return true;
            }

            /// <summary>
            /// converts a vector string value to values in a float array<br />
            /// parsing to float can throw exceptions, exception handling is the 
            /// responsibility of the user
            /// </summary>
            /// <param name="value">vector string value with # as separator</param>
            /// <returns>values of vector parsed to floats</returns>
            public static float[] StringToVectorValues(string value)
            {
                string[] split = value.Split('#');
                int count = split.Where(s => !s.IsNullOrEmpty()).Count();

                int i = 0;
                float[] vectorValues = new float[count];

                foreach (string s in split)
                {
                    if (s.IsNullOrEmpty())
                        continue;

                    // exceptions should be caught by user
                    vectorValues[i] = float.Parse(s);
                    i++;
                }

                return vectorValues;
            }

            /// <summary>
            /// converts vector values to new vector object
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

                _logger.LogError($"FloatValuesToVectorByType was supplied not supported vector type {t.Name}");
                return null;
            }

            /// <summary>
            /// converts a vector to its string format by its type
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

                _logger.LogError($"VectorToStringByType was supplied not supported vector type {t.Name}");
                return null;
            }

            public static string Vector2ToString(Vector2 v)
            {
                return $"##{v.x}#{v.y}";
            }

            public static string Vector3ToString(Vector3 v)
            {
                return $"#{v.x}#{v.y}#{v.z}";
            }

            public static string Vector4ToString(Vector4 v)
            {
                return $"{v.x}#{v.y}#{v.z}#{v.w}";
            }

            public static string QuaternionToString(Quaternion v)
            {
                return $"{v.x}#{v.y}#{v.z}#{v.w}";
            }

            public static Vector2 ValuesToVector2(float[] values)
            {
                return new(values[0], values[1]);
            }

            public static Vector3 ValuesToVector3(float[] values)
            {
                return new(values[0], values[1], values[2]);
            }

            public static Vector4 ValuesToVector4(float[] values)
            {
                return new(values[0], values[1], values[2], values[3]);
            }

            public static Quaternion ValuesToQuaternion(float[] values)
            {
                return new(values[0], values[1], values[2], values[3]);
            }
        }
    }
}
