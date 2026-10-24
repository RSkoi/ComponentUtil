using System.Linq;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

using RSkoi_ComponentUtil.Core;

namespace RSkoi_ComponentUtil
{
    internal static partial class ComponentUtilEditor
    {
        static void ConfigColor(PropertyEntryContext ctx)
        {
            var uiEntry = ctx.UiEntry;
            if (uiEntry.UiSelectable == null)
                uiEntry.UiSelectable = ctx.EntryGO.transform.Find("ColorButton").GetComponentInChildren<Button>();
            var colorButton = (Button)uiEntry.UiSelectable;
            colorButton.onClick.RemoveAllListeners();

            var colorPalette = Studio.Studio.Instance.colorPalette;
            Color GetCurrentColor() => (Color)ctx.Member.GetValue(ctx.Target);

            colorButton.interactable = ctx.Interactable;
            SetButtonColor(colorButton, GetCurrentColor());

            colorButton.onClick.AddListener(() =>
            {
                if (colorPalette.visible)
                {
                    colorPalette.visible = false;
                    return;
                }

                colorPalette.Setup($"ComponentUtil: {ctx.Member.Name} Color", GetCurrentColor(), (c) =>
                {
                    SetButtonColor(colorButton, c);
                    ApplyEdit(ctx, c);
                }, _useAlpha: true);
                colorPalette.visible = true;
            });

            uiEntry.UiComponentSetValueResetDelegate = (value) =>
            {
                var valColor = ColorConversion.StringToColor((string)value);
                SetButtonColor(colorButton, valColor);
                return valColor;
            };
        }

        static void SetButtonColor(Button button, Color color)
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
    }

    /// <summary>
    /// Provides methods to parse colors to ComponentUtil encoded format and back
    /// </summary>
    public static class ColorConversion
    {
        /// <summary>
        /// Converts a color string value to color<br/>
        /// Parsing to float can throw exceptions, exception handling is the responsibility of the user
        /// </summary>
        /// <param name="value">color string value with # as separator</param>
        /// <returns>values parsed to color</returns>
        public static Color StringToColor(string value)
        {
            var colorValue = Color.white;

            var split = value.Split('#');
            var count = split.Where(s => !string.IsNullOrEmpty(s)).Count();
            if (count != 4)
            {
                Logger.LogError("ColorConversion.StringToColor() was provided incomplete color string, returning default");
                return colorValue;
            }

            var i = 0;
            foreach (var s in split)
            {
                if (string.IsNullOrEmpty(s))
                    continue;

                // exceptions should be caught by user
                colorValue[i] = InvariantConversion.ParseFloat(s);
                i++;
            }

            return colorValue;
        }

        public static string ColorToString(Color c)
            => string.Format(CultureInfo.InvariantCulture, "{0}#{1}#{2}#{3}", c.r, c.g, c.b, c.a);
    }
}
