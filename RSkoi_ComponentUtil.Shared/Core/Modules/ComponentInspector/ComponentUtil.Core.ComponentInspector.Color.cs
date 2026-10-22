using System;
using System.Linq;
using System.Reflection;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

using RSkoi_ComponentUtil.UI;
using RSkoi_ComponentUtil.Core;

namespace RSkoi_ComponentUtil
{
    public partial class ComponentUtil
    {
        void ConfigColor(
            ComponentUtilUI.GenericUIListEntry parentUiEntry,
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
            if (uiEntry.UiSelectable == null)
                uiEntry.UiSelectable = entry.transform.Find("ColorButton").GetComponentInChildren<Button>();
            var colorButton = (Button)uiEntry.UiSelectable;
            colorButton.onClick.RemoveAllListeners();

            var colorPalette = Studio.Studio.Instance.colorPalette;
            Color GetCurrenntColor() => (Color)(isProperty ? p.GetValue(input, null) : f.GetValue(input));

            colorButton.interactable = setMethodIsPublic;
            SetButtonColor(colorButton, GetCurrenntColor());

            var propName = isProperty ? p.Name : f.Name;

            colorButton.onClick.AddListener(() =>
            {
                if (colorPalette.visible)
                {
                    colorPalette.visible = false;
                    return;
                }

                var currentColor = GetCurrenntColor();
                colorPalette.Setup($"ComponentUtil: {propName} Color", currentColor, (c) => 
                {
                    currentColor = GetCurrenntColor();
                    var colorValueString = ColorConversion.ColorToString(currentColor);

                    SetButtonColor(colorButton, c);

                    object defaultValue = colorValueString; 
                    var options = PropertyTrackerData.PropertyTrackerDataOptions.IsColor;
                    if (isProperty)
                        options |= PropertyTrackerData.PropertyTrackerDataOptions.IsProperty;

                    if (objectMode)
                    {
                        AddPropertyToTracker(_selectedObject, _selectedComponent.gameObject, _selectedComponent,
                            _selectedReferencePropertyUiEntry.PropertyNameValue, propName, defaultValue, options);
                        _selectedReferencePropertyUiEntry.SetBgColorEdited();
                    }
                    else
                        AddPropertyToTracker(_selectedObject, _selectedComponent.gameObject, _selectedComponent, propName, defaultValue, options);

                    if (isProperty)
                        SetPropertyValue(p, c, input);
                    else
                        SetFieldValue(f, c, input);

                    uiEntry.SetBgColorEdited();
                    ComponentUtilUI.TraverseAndSetEditedParents();
                }, _useAlpha: true);
                colorPalette.visible = true;
            });

            uiEntry.UiComponentSetValueResetDelegate = (value) =>
            {
                var valColor = ColorConversion.StringToColor((string)value);
                SetButtonColor(colorButton, valColor);
                return valColor;
            };
            uiEntry.ResetOverrideDelegate = (value) =>
            {
                var valColor = ColorConversion.StringToColor((string)value);
                if (isProperty)
                    SetPropertyValue(p, valColor, input);
                else
                    SetFieldValue(f, valColor, input);
                return valColor;
            };
            uiEntry.ParentUiEntry = parentUiEntry;
        }

        void SetButtonColor(Button button, Color color)
        {
            // clamp alpha, otherwise button will not be visible at all
            if (color.a < 0.1f)
                color.a = 0.1f;

            var buttonColors = button.colors;
            buttonColors.normalColor = color;
            buttonColors.highlightedColor = color;
            buttonColors.pressedColor = color;
            buttonColors.disabledColor = new(color.r, color.g, color.b, a: 0.5f);
            button.colors = buttonColors;
        }

        /// <summary>
        /// provides methods to parse colors to ComponentUtil format and back
        /// </summary>
        public static class ColorConversion
        {
            /// <summary>
            /// converts a color string value to color<br />
            /// parsing to float can throw exceptions, exception handling is the 
            /// responsibility of the user
            /// </summary>
            /// <param name="value">color string value with # as separator</param>
            /// <returns>values parsed to color</returns>
            public static Color StringToColor(string value)
            {
                var colorValue = Color.white;

                var split = value.Split('#');
                var count = split.Where(s => !s.IsNullOrEmpty()).Count();
                if (count != 4)
                {
                    _logger.LogError("ColorConversion.StringToColor() was provided incomplete color string, returning default");
                    return colorValue;
                }

                var i = 0;
                foreach (var s in split)
                {
                    if (s.IsNullOrEmpty())
                        continue;

                    // exceptions should be caught by user
                    colorValue[i] = InvariantConversion.ParseFloat(s);
                    i++;
                }

                return colorValue;
            }

            public static string ColorToString(Color c)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}#{1}#{2}#{3}", c.r, c.g, c.b, c.a);
            }
        }
    }
}
