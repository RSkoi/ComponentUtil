using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.UI;

using RSkoi_ComponentUtil.Core;

using static RSkoi_ComponentUtil.UI.ComponentUtilUI;

namespace RSkoi_ComponentUtil
{
    internal static partial class ComponentUtilEditor
    {
        static void ConfigFlagsDropdown(PropertyEntryContext ctx, Dropdown dropdown) => new FlagsDropdown(ctx, dropdown).Configure();

        /// <summary>
        /// Dropdown for a flags enum. Option 0 shows current value, other options are marked [x] / [ ] (see FlagsEnum).<br/>
        /// The option list stays open when clicking on options (see KeepOpenDropdown).<br/>
        /// Instances live as long as the dropdown's onValueChanged and the entry's UiComponentSetValueResetDelegate refer to them.
        /// </summary>
        class FlagsDropdown
        {
            const int CURRENT_VALUE_OPTION = 0;
            const int FIRST_NAME_OPTION = 1;

            readonly PropertyEntryContext _ctx;
            readonly Dropdown _dropdown;
            readonly KeepOpenFlagsDropdown _keepOpen;
            readonly FlagsEnum _flagsEnum;

            internal FlagsDropdown(PropertyEntryContext ctx, Dropdown dropdown)
            {
                _ctx = ctx;
                _dropdown = dropdown;
                if (ctx.UiEntry.KeepOpen == null)
                    ctx.UiEntry.KeepOpen = dropdown.gameObject.AddComponent<KeepOpenFlagsDropdown>();
                _keepOpen = ctx.UiEntry.KeepOpen;
                _flagsEnum = new FlagsEnum(ctx.Member.MemberType);
            }

            internal void Configure()
            {
                var options = new List<Dropdown.OptionData>();
                for (var i = 0; i < FIRST_NAME_OPTION + _flagsEnum.Names.Length; i++)
                    options.Add(new Dropdown.OptionData());
                _dropdown.ClearOptions();
                _dropdown.AddOptions(options);

                _keepOpen.OnOptionClicked = OnOptionClicked;
                _dropdown.onValueChanged.AddListener(OnOptionClicked);
                _ctx.UiEntry.UiComponentSetValueResetDelegate = ResetTo;

                Show(EnumConversion.ToBits(_ctx.Value));
            }

            void OnOptionClicked(int optionIndex)
            {
                if (optionIndex == CURRENT_VALUE_OPTION)
                    return;

                var currentEnumValue = _ctx.Member.TryGetValue(_ctx.Target);
                if (currentEnumValue == null)
                {
                    _dropdown.value = CURRENT_VALUE_OPTION;
                    return;
                }
                var currentValue = EnumConversion.ToBits(currentEnumValue);
                var newValue = FlagsEnum.ToggleFlag(currentValue, _flagsEnum.Values[NameIndexOf(optionIndex)]);
                ApplyEdit(_ctx, _flagsEnum.ToEnum(newValue));
                Show(newValue);
            }

            /// <param name="value">the tracked default enum value, encoded</param>
            object ResetTo(object value)
            {
                var enumValue = _flagsEnum.ToEnum(EnumConversion.ToBits(value));
                Show(EnumConversion.ToBits(enumValue));
                return enumValue;
            }

            /// <summary>
            /// Updates option texts and selects first option.
            /// </summary>
            void Show(ulong value)
            {
                _dropdown.options[CURRENT_VALUE_OPTION].text = _flagsEnum.Describe(value);
                for (var optionIndex = FIRST_NAME_OPTION; optionIndex < _dropdown.options.Count; optionIndex++)
                    _dropdown.options[optionIndex].text = _flagsEnum.DescribeName(NameIndexOf(optionIndex), value);

                _dropdown.value = CURRENT_VALUE_OPTION;
                _dropdown.RefreshShownValue();
                _keepOpen.RefreshOpenList();
            }

            static int NameIndexOf(int optionIndex) => optionIndex - FIRST_NAME_OPTION;
        }

        internal class FlagsEnum
        {
            internal readonly Type EnumType;
            internal readonly string[] Names;
            internal readonly ulong[] Values;

            internal FlagsEnum(Type enumType)
            {
                EnumType = enumType;
                Names = Enum.GetNames(enumType);
                // get values based on names due to potentially shared values between enum options
                Values = [.. Names.Select(name => EnumConversion.ToBits(Enum.Parse(enumType, name)))];
            }

            internal static bool IsFlagsEnum(Type type)
                => type.IsEnum && type.IsDefined(typeof(FlagsAttribute), inherit: false);

            internal static bool FlagIsSet(ulong value, ulong flag) => flag == 0
                ? value == 0
                : (value & flag) == flag;

            internal static ulong ToggleFlag(ulong value, ulong flag) => flag == 0 ? 0
                : FlagIsSet(value, flag)
                    ? value & ~flag
                    : value | flag;

            internal object ToEnum(ulong value) => EnumConversion.ToEnum(EnumType, value);

            internal string Describe(ulong value) => ToEnum(value).ToString().Replace(", ", " | ");
            internal string DescribeName(int nameIndex, ulong value) => $"{(FlagIsSet(value, Values[nameIndex]) ? "[x]" : "[ ]")} {Names[nameIndex]}";
        }
    }
}
