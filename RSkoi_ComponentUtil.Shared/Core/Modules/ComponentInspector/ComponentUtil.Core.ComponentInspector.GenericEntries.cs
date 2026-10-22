using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using RSkoi_ComponentUtil.UI;
using RSkoi_ComponentUtil.Core;
using RSkoi_ComponentUtil.Timeline;

namespace RSkoi_ComponentUtil
{
    public partial class ComponentUtil
    {
        /// <summary>
        /// Everything a property entry in the Component- or ObjectInspector needs to know about the member it edits.
        /// </summary>
        internal sealed class PropertyEntryContext(
            ComponentUtilUI.GenericUIListEntry parentUiEntry,
            ComponentUtilUI.PropertyUIEntry uiEntry,
            object target,
            Member member,
            object value,
            PropertyTrackerData.PropertyTrackerDataOptions flags,
            bool objectMode)
        {
            /// <summary>
            /// The selected component entry.
            /// </summary>
            public readonly ComponentUtilUI.GenericUIListEntry ParentUiEntry = parentUiEntry;
            /// <summary>
            /// The property / field entry.
            /// </summary>
            public readonly ComponentUtilUI.PropertyUIEntry UiEntry = uiEntry;
            /// <summary>
            /// The selected component (ComponentInspector) or the referenced object (ObjectInspector) the member belongs to.
            /// </summary>
            public readonly object Target = target;
            /// <summary>
            /// The concrete member behind the property / field entry.
            /// </summary>
            public readonly Member Member = member;
            /// <summary>
            /// Value of the member when the entry was configured.
            /// </summary>
            public readonly object Value = value;
            /// <summary>
            /// Tracker flags of the member: entry kind flag, plus IsProperty for properties.
            /// </summary>
            public readonly PropertyTrackerData.PropertyTrackerDataOptions Flags = flags;
            /// <summary>
            /// Whether the entry is in the ObjectInspector (true) or ComponentInspector (false).
            /// </summary>
            public readonly bool ObjectMode = objectMode;

            public bool Interactable => Member.HasPublicSetter;
            public GameObject EntryGO => UiEntry.UiGO;
        }

        /// <summary>
        /// Adds the edit to the tracker (keeping the first default value), sets the new value, marks the entry as edited.
        /// </summary>
        /// <param name="newValue">the new value, either of the member type or encoded (see ValueConversion)</param>
        void ApplyEdit(PropertyEntryContext ctx, object newValue)
        {
            var defaultValue = ValueConversion.Encode(ctx.Member.TryGetValue(ctx.Target), ctx.Flags);
            if (ctx.ObjectMode)
            {
                AddPropertyToTracker(_selectedObject, _selectedComponent.gameObject, _selectedComponent,
                    _selectedReferencePropertyUiEntry.MemberName, ctx.Member.Name, defaultValue, ctx.Flags);
                _selectedReferencePropertyUiEntry.SetBgColorEdited();
            }
            else
                AddPropertyToTracker(_selectedObject, _selectedComponent.gameObject, _selectedComponent, ctx.Member.Name, defaultValue, ctx.Flags);

            SetValueWithFlags(ctx.Member, ctx.Target, newValue, ctx.Flags);

            ctx.UiEntry.SetBgColorEdited();
            ComponentUtilUI.TraverseAndSetEditedParents();
        }

        void ConfigReference(PropertyEntryContext ctx)
        {
            var uiEntry = ctx.UiEntry;
            if (uiEntry.UiSelectable == null)
                uiEntry.UiSelectable = ctx.EntryGO.GetComponentInChildren<Button>();
            var button = (Button)uiEntry.UiSelectable;
            button.onClick.RemoveAllListeners();

            // whether the member has a setter is irrelevant here because values inside the reference will be set with reflection
            button.interactable = true;
            button.onClick.AddListener(() => OpenObjectInspector(ctx.Member, ctx.Value, uiEntry, ctx.ParentUiEntry));

            uiEntry.UiComponentSetValueResetDelegate = null;
            uiEntry.ParentUiEntry = ctx.ParentUiEntry;

            // resetting or animating a reference as a whole is not supported, only the values inside it
            uiEntry.ResetButton.interactable = false;
            if (ComponentUtilTimeline.IsTimelineAvailable())
                uiEntry.TimelineButton.interactable = false;
            else
                uiEntry.TimelineButton.gameObject.SetActive(false);
        }

        void ConfigDropdown(PropertyEntryContext ctx)
        {
            var uiEntry = ctx.UiEntry;
            if (uiEntry.UiSelectable == null)
                uiEntry.UiSelectable = ctx.EntryGO.GetComponentInChildren<Dropdown>();
            var dropdownField = (Dropdown)uiEntry.UiSelectable;
            dropdownField.onValueChanged.RemoveAllListeners();

            // configure options
            // dropdown works with option indices, which are not the same as enum values
            // (e.g. flag enums 0, 1, 2, 4 or enums containing negative values) -> map between them
            var enumValues = Enum.GetValues(ctx.Member.MemberType);
            var options = new List<Dropdown.OptionData>();
            var selectedOption = 0;
            for (var i = 0; i < enumValues.Length; i++)
            {
                var enumValue = enumValues.GetValue(i);
                options.Add(new() { text = enumValue.ToString() });

                // option that's currently selected
                if (enumValue.Equals(ctx.Value))
                    selectedOption = i;
            }
            int EnumValueOfOption(int optionIndex) => Convert.ToInt32(enumValues.GetValue(optionIndex));
            int OptionOfEnumValue(int enumValue)
            {
                for (var i = 0; i < enumValues.Length; i++)
                    if (Convert.ToInt32(enumValues.GetValue(i)) == enumValue)
                        return i;
                return 0; // fallback to first option
            }
            dropdownField.ClearOptions();
            dropdownField.AddOptions(options);
            dropdownField.interactable = ctx.Interactable;

            // select current option
            dropdownField.value = selectedOption;

            void registerMyEvents()
                => dropdownField.onValueChanged.AddListener(optionIndex => ApplyEdit(ctx, EnumValueOfOption(optionIndex)));
            registerMyEvents();

            uiEntry.UiComponentSetValueResetDelegate = (value) =>
            {
                // suspending all (non-persistent) listeners because dropdownField.value
                // will trigger onValueChanged, which would add to tracker
                dropdownField.onValueChanged.RemoveAllListeners();
                // value is the tracked default, i.e. an enum value, not an option index
                var enumValue = Convert.ToInt32(value);
                dropdownField.value = OptionOfEnumValue(enumValue);
                registerMyEvents();
                return enumValue;
            };
            uiEntry.ParentUiEntry = ctx.ParentUiEntry;
        }

        void ConfigToggle(PropertyEntryContext ctx)
        {
            var uiEntry = ctx.UiEntry;
            if (uiEntry.UiSelectable == null)
                uiEntry.UiSelectable = ctx.EntryGO.GetComponentInChildren<Toggle>();
            var toggleField = (Toggle)uiEntry.UiSelectable;
            toggleField.onValueChanged.RemoveAllListeners();

            toggleField.isOn = (bool)ctx.Value;
            toggleField.interactable = ctx.Interactable;

            void registerMyEvents()
                => toggleField.onValueChanged.AddListener(isOn => ApplyEdit(ctx, isOn));
            registerMyEvents();

            uiEntry.UiComponentSetValueResetDelegate = (value) =>
            {
                // suspending all (non-persistent) listeners because toggleField.isOn
                // will trigger onValueChanged, which would add to tracker
                toggleField.onValueChanged.RemoveAllListeners();
                toggleField.isOn = (bool)value;
                registerMyEvents();
                return toggleField.isOn;
            };
            uiEntry.ParentUiEntry = ctx.ParentUiEntry;
        }

        void ConfigInput(PropertyEntryContext ctx, InputField.ContentType contentType)
        {
            var uiEntry = ctx.UiEntry;
            if (uiEntry.UiSelectable == null)
                uiEntry.UiSelectable = ctx.EntryGO.GetComponentInChildren<InputField>();
            var inputField = (InputField)uiEntry.UiSelectable;
            inputField.onValueChanged.RemoveAllListeners();

            inputField.contentType = contentType;
            inputField.text = InvariantConversion.ToInvariantString(ctx.Value);
            inputField.interactable = ctx.Interactable;

            void registerMyEvents()
                => inputField.onValueChanged.AddListener(text => ApplyEdit(ctx, text));
            registerMyEvents();

            uiEntry.UiComponentSetValueResetDelegate = (value) =>
            {
                // suspending all (non-persistent) listeners because inputField.text
                // will trigger onValueChanged, which would add to tracker
                inputField.onValueChanged.RemoveAllListeners();
                inputField.text = InvariantConversion.ToInvariantString(value);
                registerMyEvents();
                return inputField.text;
            };
            uiEntry.ParentUiEntry = ctx.ParentUiEntry;
        }

        #region reset button
        void ConfigReset(PropertyKey key, PropertyEntryContext ctx)
        {
            var uiEntry = ctx.UiEntry;
            var propName = ctx.Member.Name;
            uiEntry.ResetButton.interactable = ctx.Interactable;
            uiEntry.ResetButton.onClick.RemoveAllListeners();
            uiEntry.ResetButton.onClick.AddListener(() =>
            {
                if (!PropertyIsTracked(key, propName))
                    return;

                var defaultValue = GetTrackedDefaultValue(key, propName);
                SetValueWithFlags(ctx.Member, ctx.Target, defaultValue, ctx.Flags);

                RemovePropertyFromTracker(key, propName);
                uiEntry.SetUiComponentTargetValue(defaultValue);
                uiEntry.SetBgColorDefault();
                ComponentUtilUI.TraverseAndSetEditedParents();
            });
        }

        void ConfigResetReferenceProperty(PropertyReferenceKey key, PropertyEntryContext ctx)
        {
            var uiEntry = ctx.UiEntry;
            var propName = ctx.Member.Name;
            uiEntry.ResetButton.interactable = ctx.Interactable;
            uiEntry.ResetButton.onClick.RemoveAllListeners();
            uiEntry.ResetButton.onClick.AddListener(() =>
            {
                if (!PropertyIsTracked(key, propName))
                    return;

                var defaultValue = GetTrackedDefaultValue(key, propName);
                SetValueWithFlags(ctx.Member, ctx.Target, defaultValue, ctx.Flags);

                RemovePropertyFromTracker(key, propName, out bool removedKey);
                uiEntry.SetUiComponentTargetValue(defaultValue);
                uiEntry.SetBgColorDefault();
                if (removedKey)
                    _selectedReferencePropertyUiEntry.SetBgColorDefault();
                ComponentUtilUI.TraverseAndSetEditedParents();
            });
        }
        #endregion reset button

        #region timeline button
        void ConfigTimeline(PropertyKey key, PropertyEntryContext ctx)
        {
            if (!ConfigTimelineButton(ctx))
                return;
            ctx.UiEntry.TimelineButton.onClick.AddListener(
                () => ComponentUtilTimeline.SelectTimelineModelTarget(key, ctx.Member, ctx.Flags));
        }

        void ConfigTimelineReferenceProperty(PropertyReferenceKey key, PropertyEntryContext ctx)
        {
            if (!ConfigTimelineButton(ctx))
                return;
            ctx.UiEntry.TimelineButton.onClick.AddListener(
                () => ComponentUtilTimeline.SelectTimelineModelTarget(key, ctx.Target, ctx.Member, ctx.Flags));
        }

        /// <returns>false if Timeline is not available and the button was hidden</returns>
        bool ConfigTimelineButton(PropertyEntryContext ctx)
        {
            var button = ctx.UiEntry.TimelineButton;
            if (!ComponentUtilTimeline.IsTimelineAvailable())
            {
                button.gameObject.SetActive(false);
                return false;
            }

            button.interactable = ctx.Interactable;
            button.onClick.RemoveAllListeners();
            return true;
        }
        #endregion timeline button
    }
}
