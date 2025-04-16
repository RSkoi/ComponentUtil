using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

using RSkoi_ComponentUtil.UI;

namespace RSkoi_ComponentUtil
{
    public partial class ComponentUtil
    {
        private void ConfigColor(
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
            Button colorButton = (Button)uiEntry.UiSelectable;
            colorButton.onClick.RemoveAllListeners();

            Studio.ColorPalette colorPalette = Studio.Studio.Instance.colorPalette;
            Color GetCurColor()
            {
                return (Color)(isProperty ? p.GetValue(input, null) : f.GetValue(input));
            };

            colorButton.interactable = setMethodIsPublic;
            SetButtonColor(colorButton, GetCurColor());

            string propName = isProperty ? p.Name : f.Name;

            colorButton.onClick.AddListener(() =>
            {
                if (colorPalette.visible)
                {
                    colorPalette.visible = false;
                    return;
                }

                Color curColor = GetCurColor();
                colorPalette.Setup($"ComponentUtil: {propName} Color", curColor, (c) => 
                {
                    curColor = GetCurColor();
                    string colorValueString = ColorConversion.ColorToString(curColor);

                    SetButtonColor(colorButton, c);

                    object defaultValue = colorValueString; 
                    PropertyTrackerData.PropertyTrackerDataOptions options = PropertyTrackerData.PropertyTrackerDataOptions.IsColor;
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
                }, true);
                colorPalette.visible = true;
            });

            uiEntry.UiComponentSetValueResetDelegate = (value) =>
            {
                Color valColor = ColorConversion.StringToColor((string)value);
                SetButtonColor(colorButton, valColor);
                return valColor;
            };
            uiEntry.ResetOverrideDelegate = (value) =>
            {
                Color valColor = ColorConversion.StringToColor((string)value);
                if (isProperty)
                    SetPropertyValue(p, valColor, input);
                else
                    SetFieldValue(f, valColor, input);
                return valColor;
            };
            uiEntry.ParentUiEntry = parentUiEntry;
        }

        private void SetButtonColor(Button button, Color color)
        {
            // clamp alpha, otherwise button will not be visible at all
            if (color.a < 0.1f)
                color.a = 0.1f;

            ColorBlock buttonColors = button.colors;
            buttonColors.normalColor = color;
            buttonColors.highlightedColor = color;
            buttonColors.pressedColor = color;
            buttonColors.disabledColor = new(color.r, color.g, color.b, 0.5f);
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
                Color colorValue = Color.white;

                string[] split = value.Split('#');
                int count = split.Where(s => !s.IsNullOrEmpty()).Count();
                if (count != 4)
                {
                    _logger.LogError("ColorConversion.StringToColor() was provided incomplete color string, returning default");
                    return colorValue;
                }

                int i = 0;
                foreach (string s in split)
                {
                    if (s.IsNullOrEmpty())
                        continue;

                    // exceptions should be caught by user
                    colorValue[i] = float.Parse(s);
                    i++;
                }

                return colorValue;
            }

            public static string ColorToString(Color c)
            {
                return $"{c.r}#{c.g}#{c.b}#{c.a}";
            }
        }
    }
}
