﻿using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using RSkoi_ComponentUtil.UI;
using RSkoi_ComponentUtil.Timeline;

namespace RSkoi_ComponentUtil
{
    public partial class ComponentUtil
    {
        private void ConfigReference(
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
            Button button = (Button)uiEntry.UiSelectable;
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

        private void ConfigDropdown(
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
            Dropdown dropdownField = (Dropdown)uiEntry.UiSelectable;
            dropdownField.onValueChanged.RemoveAllListeners();

            // configure options
            List<Dropdown.OptionData> options = [];
            int selectedOption = 0;
            int i = 0;
            foreach (var enumValue in Enum.GetValues(type))
            {
                Dropdown.OptionData option = new() { text = enumValue.ToString() };
                options.Add(option);

                // option that's currently selected
                if (enumValue.Equals(value))
                    selectedOption = i;

                i++;
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
                    dropdownField.onValueChanged.AddListener(value =>
                    {
                        int defaultValue = (int)p.GetValue(input, null);
                        if (objectMode)
                        {
                            AddPropertyToTracker(_selectedObject, _selectedComponent.gameObject, _selectedComponent, _selectedReferencePropertyUiEntry.PropertyNameValue,
                                p.Name, defaultValue, PropertyTrackerData.PropertyTrackerDataOptions.IsProperty | PropertyTrackerData.PropertyTrackerDataOptions.IsInt);
                            _selectedReferencePropertyUiEntry.SetBgColorEdited();
                        }
                        else
                            AddPropertyToTracker(_selectedObject, _selectedComponent.gameObject, _selectedComponent, p.Name, defaultValue,
                                PropertyTrackerData.PropertyTrackerDataOptions.IsProperty | PropertyTrackerData.PropertyTrackerDataOptions.IsInt);
                        SetPropertyValueInt(p, value, input);
                    });
                else
                    dropdownField.onValueChanged.AddListener(value =>
                    {
                        int defaultValue = (int)f.GetValue(input);
                        if (objectMode)
                        {
                            AddPropertyToTracker(_selectedObject, _selectedComponent.gameObject, _selectedComponent, _selectedReferencePropertyUiEntry.PropertyNameValue,
                                f.Name, defaultValue, PropertyTrackerData.PropertyTrackerDataOptions.IsInt);
                            _selectedReferencePropertyUiEntry.SetBgColorEdited();
                        }
                        else
                            AddPropertyToTracker(_selectedObject, _selectedComponent.gameObject, _selectedComponent, f.Name, defaultValue,
                                PropertyTrackerData.PropertyTrackerDataOptions.IsInt);
                        SetFieldValueInt(f, value, input);
                    });

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
                dropdownField.value = (int)value;
                registerMyEvents();
                return dropdownField.value;
            };
            uiEntry.ParentUiEntry = parentUiEntry;
        }

        private void ConfigToggle(
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
            Toggle toggleField = (Toggle)uiEntry.UiSelectable;
            toggleField.onValueChanged.RemoveAllListeners();

            toggleField.isOn = (bool)value;
            toggleField.interactable = setMethodIsPublic;
            void registerMyEvents()
            {
                if (isProperty)
                    toggleField.onValueChanged.AddListener(value =>
                    {
                        object defaultValue = p.GetValue(input, null);
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
                        object defaultValue = f.GetValue(input);
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

        private void ConfigInput(
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
            InputField inputField = (InputField)uiEntry.UiSelectable;
            inputField.onValueChanged.RemoveAllListeners();

            inputField.contentType = contentType;
            inputField.text = value.ToString();
            inputField.interactable = setMethodIsPublic;

            void registerMyEvents()
            {
                if (isProperty)
                    inputField.onValueChanged.AddListener(value =>
                    {
                        object defaultValue = p.GetValue(input, null);
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
                        object defaultValue = f.GetValue(input);
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
                inputField.text = value.ToString();
                registerMyEvents();
                return inputField.text;
            };

            uiEntry.ParentUiEntry = parentUiEntry;
        }

        #region reset button
        private void ConfigReset(
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

                object defaultValue = GetTrackedDefaultValue(key, propName);
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

        private void ConfigResetReference(
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

        private void ConfigResetReferenceProperty(
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

                object defaultValue = GetTrackedDefaultValue(key, propName);
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
        private void ConfigTimeline(
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

            PropertyTrackerData.PropertyTrackerDataOptions options = typeAsFlag;
            if (isProperty)
                options |= PropertyTrackerData.PropertyTrackerDataOptions.IsProperty;
            uiEntry.TimelineButton.interactable = setMethodIsPublic;
            uiEntry.TimelineButton.onClick.RemoveAllListeners();
            uiEntry.TimelineButton.onClick.AddListener(() => ComponentUtilTimeline.SelectTimelineModelTarget(key, p, f, propName, options));
        }

        private void ConfigTimelineReference(
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

        private void ConfigTimelineReferenceProperty(
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

            PropertyTrackerData.PropertyTrackerDataOptions options = typeAsFlag;
            if (isProperty)
                options |= PropertyTrackerData.PropertyTrackerDataOptions.IsProperty;
            uiEntry.TimelineButton.interactable = setMethodIsPublic;
            uiEntry.TimelineButton.onClick.RemoveAllListeners();
            uiEntry.TimelineButton.onClick.AddListener(() => ComponentUtilTimeline.SelectTimelineModelTarget(key, input, p, f, propName, options));
        }
        #endregion timeline button
    }
}
