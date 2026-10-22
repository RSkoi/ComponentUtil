using System;
using System.Reflection;
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
        void ConfigReference(
            ComponentUtilUI.GenericUIListEntry parentUiEntry,
            GameObject entry,
            ComponentUtilUI.PropertyUIEntry uiEntry,
            Component input,
            PropertyInfo p,
            FieldInfo f,
            Type type,
            bool setMethodIsPublic,
            bool isProperty,
            object value)
        {
            if (uiEntry.UiSelectable == null)
                uiEntry.UiSelectable = entry.GetComponentInChildren<Button>();
            var button = (Button)uiEntry.UiSelectable;
            button.onClick.RemoveAllListeners();

            // setMethodIsPublic is irrelevant here because values will be set with reflection
            button.interactable = true;

            void registerMyEvents()
            {
                if (isProperty)
                    button.onClick.AddListener(() => OpenObjectInspector(p, null, type, value, uiEntry, parentUiEntry));
                else
                    button.onClick.AddListener(() => OpenObjectInspector(null, f, type, value, uiEntry, parentUiEntry));
            }
            registerMyEvents();

            uiEntry.UiComponentSetValueResetDelegate = null;
            uiEntry.ParentUiEntry = parentUiEntry;
        }

        void ConfigDropdown(
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
                uiEntry.UiSelectable = entry.GetComponentInChildren<Dropdown>();
            var dropdownField = (Dropdown)uiEntry.UiSelectable;
            dropdownField.onValueChanged.RemoveAllListeners();

            // configure options
            // dropdown works with option indices, which are not the same as enum values
            // (e.g. flag enums 0, 1, 2, 4 or enums containing negative values) -> map between them
            var enumValues = Enum.GetValues(type);
            var options = new List<Dropdown.OptionData>();
            var selectedOption = 0;
            for (var i = 0; i < enumValues.Length; i++)
            {
                var enumValue = enumValues.GetValue(i);
                options.Add(new() { text = enumValue.ToString() });

                // option that's currently selected
                if (enumValue.Equals(value))
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
            dropdownField.interactable = setMethodIsPublic;

            // select current option
            dropdownField.value = selectedOption;

            // configure value change event
            void registerMyEvents()
            {
                if (isProperty)
                {
                    dropdownField.onValueChanged.AddListener(optionIndex =>
                    {
                        var defaultValue = Convert.ToInt32(p.GetValue(input, null));
                        if (objectMode)
                        {
                            AddPropertyToTracker(_selectedObject, _selectedComponent.gameObject, _selectedComponent, _selectedReferencePropertyUiEntry.PropertyNameValue,
                                p.Name, defaultValue, PropertyTrackerData.PropertyTrackerDataOptions.IsProperty | PropertyTrackerData.PropertyTrackerDataOptions.IsInt);
                            _selectedReferencePropertyUiEntry.SetBgColorEdited();
                        }
                        else
                            AddPropertyToTracker(_selectedObject, _selectedComponent.gameObject, _selectedComponent, p.Name, defaultValue,
                                PropertyTrackerData.PropertyTrackerDataOptions.IsProperty | PropertyTrackerData.PropertyTrackerDataOptions.IsInt);

                        var enumValue = EnumValueOfOption(optionIndex);
                        SetPropertyValueInt(p, enumValue, input);
                    });
                }
                else
                {
                    dropdownField.onValueChanged.AddListener(optionIndex =>
                    {
                        var defaultValue = Convert.ToInt32(f.GetValue(input));
                        if (objectMode)
                        {
                            AddPropertyToTracker(_selectedObject, _selectedComponent.gameObject, _selectedComponent, _selectedReferencePropertyUiEntry.PropertyNameValue,
                                f.Name, defaultValue, PropertyTrackerData.PropertyTrackerDataOptions.IsInt);
                            _selectedReferencePropertyUiEntry.SetBgColorEdited();
                        }
                        else
                            AddPropertyToTracker(_selectedObject, _selectedComponent.gameObject, _selectedComponent, f.Name, defaultValue,
                                PropertyTrackerData.PropertyTrackerDataOptions.IsInt);

                        var enumValue = EnumValueOfOption(optionIndex);
                        SetFieldValueInt(f, enumValue, input);
                    });
                }

                dropdownField.onValueChanged.AddListener(_ =>
                {
                    uiEntry.SetBgColorEdited();
                    ComponentUtilUI.TraverseAndSetEditedParents();
                });
            }
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
            uiEntry.ParentUiEntry = parentUiEntry;
        }

        void ConfigToggle(
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
                uiEntry.UiSelectable = entry.GetComponentInChildren<Toggle>();
            var toggleField = (Toggle)uiEntry.UiSelectable;
            toggleField.onValueChanged.RemoveAllListeners();

            toggleField.isOn = (bool)value;
            toggleField.interactable = setMethodIsPublic;
            void registerMyEvents()
            {
                if (isProperty)
                    toggleField.onValueChanged.AddListener(value =>
                    {
                        var defaultValue = p.GetValue(input, null);
                        if (objectMode)
                        {
                            AddPropertyToTracker(_selectedObject, _selectedComponent.gameObject, _selectedComponent, _selectedReferencePropertyUiEntry.PropertyNameValue,
                                p.Name, defaultValue, PropertyTrackerData.PropertyTrackerDataOptions.IsProperty | PropertyTrackerData.PropertyTrackerDataOptions.IsBool);
                            _selectedReferencePropertyUiEntry.SetBgColorEdited();
                        }
                        else
                            AddPropertyToTracker(_selectedObject, _selectedComponent.gameObject, _selectedComponent, p.Name, defaultValue,
                                PropertyTrackerData.PropertyTrackerDataOptions.IsProperty | PropertyTrackerData.PropertyTrackerDataOptions.IsBool);
                        SetPropertyValue(p, value.ToString(), input);
                    });
                else
                    toggleField.onValueChanged.AddListener(value =>
                    {
                        var defaultValue = f.GetValue(input);
                        if (objectMode)
                        {
                            AddPropertyToTracker(_selectedObject, _selectedComponent.gameObject, _selectedComponent, _selectedReferencePropertyUiEntry.PropertyNameValue,
                                f.Name, defaultValue, PropertyTrackerData.PropertyTrackerDataOptions.IsBool);
                            _selectedReferencePropertyUiEntry.SetBgColorEdited();
                        }
                        else
                            AddPropertyToTracker(_selectedObject, _selectedComponent.gameObject, _selectedComponent, f.Name, defaultValue,
                                PropertyTrackerData.PropertyTrackerDataOptions.IsBool);
                        SetFieldValue(f, value.ToString(), input);
                    });
                toggleField.onValueChanged.AddListener(_ =>
                {
                    uiEntry.SetBgColorEdited();
                    ComponentUtilUI.TraverseAndSetEditedParents();
                });
            }
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
            uiEntry.ParentUiEntry = parentUiEntry;
        }

        void ConfigInput(
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
            bool objectMode,
            InputField.ContentType contentType)
        {
            if (uiEntry.UiSelectable == null)
                uiEntry.UiSelectable = entry.GetComponentInChildren<InputField>();
            var inputField = (InputField)uiEntry.UiSelectable;
            inputField.onValueChanged.RemoveAllListeners();

            inputField.contentType = contentType;
            inputField.text = InvariantConversion.ToInvariantString(value);
            inputField.interactable = setMethodIsPublic;

            void registerMyEvents()
            {
                if (isProperty)
                    inputField.onValueChanged.AddListener(value =>
                    {
                        var defaultValue = p.GetValue(input, null);
                        if (objectMode)
                        {
                            AddPropertyToTracker(_selectedObject, _selectedComponent.gameObject, _selectedComponent, _selectedReferencePropertyUiEntry.PropertyNameValue,
                                p.Name, defaultValue, PropertyTrackerData.PropertyTrackerDataOptions.IsProperty | PropertyTrackerData.PropertyTrackerDataOptions.IsInput);
                            _selectedReferencePropertyUiEntry.SetBgColorEdited();
                        }
                        else
                            AddPropertyToTracker(_selectedObject, _selectedComponent.gameObject, _selectedComponent, p.Name, defaultValue,
                                PropertyTrackerData.PropertyTrackerDataOptions.IsProperty | PropertyTrackerData.PropertyTrackerDataOptions.IsInput);
                        SetPropertyValue(p, value, input);
                    });
                else
                    inputField.onValueChanged.AddListener(value =>
                    {
                        var defaultValue = f.GetValue(input);
                        if (objectMode)
                        {
                            AddPropertyToTracker(_selectedObject, _selectedComponent.gameObject, _selectedComponent, _selectedReferencePropertyUiEntry.PropertyNameValue,
                                f.Name, defaultValue, PropertyTrackerData.PropertyTrackerDataOptions.IsInput);
                            _selectedReferencePropertyUiEntry.SetBgColorEdited();
                        }
                        else
                            AddPropertyToTracker(_selectedObject, _selectedComponent.gameObject, _selectedComponent, f.Name, defaultValue,
                                PropertyTrackerData.PropertyTrackerDataOptions.IsInput);
                        SetFieldValue(f, value, input);
                    });
                inputField.onValueChanged.AddListener(_ =>
                {
                    uiEntry.SetBgColorEdited();
                    ComponentUtilUI.TraverseAndSetEditedParents();
                });
            }
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

            uiEntry.ParentUiEntry = parentUiEntry;
        }

        #region reset button
        void ConfigReset(
            PropertyKey key,
            ComponentUtilUI.PropertyUIEntry uiEntry,
            Component input,
            PropertyInfo p,
            FieldInfo f,
            string propName,
            bool setMethodIsPublic,
            bool isProperty)
        {
            uiEntry.ResetButton.interactable = setMethodIsPublic;
            uiEntry.ResetButton.onClick.RemoveAllListeners();
            uiEntry.ResetButton.onClick.AddListener(() =>
            {
                if (!PropertyIsTracked(key, propName))
                    return;

                var defaultValue = GetTrackedDefaultValue(key, propName);
                if (uiEntry.ResetOverrideDelegate != null)
                    uiEntry.ResetOverrideDelegate.Invoke(defaultValue);
                else if (isProperty)
                    p.SetValue(input, defaultValue, null);
                else
                    f.SetValue(input, defaultValue);

                RemovePropertyFromTracker(key, propName);
                uiEntry.SetUiComponentTargetValue(defaultValue);
                uiEntry.SetBgColorDefault();
                ComponentUtilUI.TraverseAndSetEditedParents();
            });
        }

        void ConfigResetReference(
            PropertyReferenceKey key,
            ComponentUtilUI.PropertyUIEntry uiEntry,
            object input,
            PropertyInfo p,
            FieldInfo f,
            string propName,
            bool isProperty)
        {
            // fuck this
            uiEntry.ResetButton.interactable = false;
        }

        void ConfigResetReferenceProperty(
            PropertyReferenceKey key,
            ComponentUtilUI.PropertyUIEntry uiEntry,
            object input,
            PropertyInfo p,
            FieldInfo f,
            string propName,
            bool setMethodIsPublic,
            bool isProperty)
        {
            uiEntry.ResetButton.interactable = setMethodIsPublic;
            uiEntry.ResetButton.onClick.RemoveAllListeners();
            uiEntry.ResetButton.onClick.AddListener(() =>
            {
                if (!PropertyIsTracked(key, propName))
                    return;

                var defaultValue = GetTrackedDefaultValue(key, propName);
                if (uiEntry.ResetOverrideDelegate != null)
                    uiEntry.ResetOverrideDelegate.Invoke(defaultValue);
                else if (isProperty)
                    p.SetValue(input, defaultValue, null);
                else
                    f.SetValue(input, defaultValue);

                RemovePropertyFromTracker(key, propName, out var removedKey);
                uiEntry.SetUiComponentTargetValue(defaultValue);
                uiEntry.SetBgColorDefault();
                if (removedKey)
                    _selectedReferencePropertyUiEntry.SetBgColorDefault();
                ComponentUtilUI.TraverseAndSetEditedParents();
            });
        }
        #endregion reset button

        #region timeline button
        void ConfigTimeline(
            PropertyKey key,
            ComponentUtilUI.PropertyUIEntry uiEntry,
            Component input,
            PropertyInfo p,
            FieldInfo f,
            string propName,
            bool setMethodIsPublic,
            bool isProperty,
            PropertyTrackerData.PropertyTrackerDataOptions typeAsFlag)
        {
            if (!ComponentUtilTimeline.IsTimelineAvailable())
            {
                uiEntry.TimelineButton.gameObject.SetActive(false);
                return;
            }

            var options = typeAsFlag;
            if (isProperty)
                options |= PropertyTrackerData.PropertyTrackerDataOptions.IsProperty;
            uiEntry.TimelineButton.interactable = setMethodIsPublic;
            uiEntry.TimelineButton.onClick.RemoveAllListeners();
            uiEntry.TimelineButton.onClick.AddListener(() => ComponentUtilTimeline.SelectTimelineModelTarget(key, p, f, propName, options));
        }

        void ConfigTimelineReference(
            PropertyReferenceKey key,
            ComponentUtilUI.PropertyUIEntry uiEntry,
            object input,
            PropertyInfo p,
            FieldInfo f,
            string propName,
            bool isProperty,
            PropertyTrackerData.PropertyTrackerDataOptions typeAsFlag)
        {
            if (!ComponentUtilTimeline.IsTimelineAvailable())
            {
                uiEntry.TimelineButton.gameObject.SetActive(false);
                return;
            }

            uiEntry.TimelineButton.interactable = false;
        }

        void ConfigTimelineReferenceProperty(
            PropertyReferenceKey key,
            ComponentUtilUI.PropertyUIEntry uiEntry,
            object input,
            PropertyInfo p,
            FieldInfo f,
            string propName,
            bool setMethodIsPublic,
            bool isProperty,
            PropertyTrackerData.PropertyTrackerDataOptions typeAsFlag)
        {
            if (!ComponentUtilTimeline.IsTimelineAvailable())
            {
                uiEntry.TimelineButton.gameObject.SetActive(false);
                return;
            }

            var options = typeAsFlag;
            if (isProperty)
                options |= PropertyTrackerData.PropertyTrackerDataOptions.IsProperty;
            uiEntry.TimelineButton.interactable = setMethodIsPublic;
            uiEntry.TimelineButton.onClick.RemoveAllListeners();
            uiEntry.TimelineButton.onClick.AddListener(() => ComponentUtilTimeline.SelectTimelineModelTarget(key, input, p, f, propName, options));
        }
        #endregion timeline button
    }
}
