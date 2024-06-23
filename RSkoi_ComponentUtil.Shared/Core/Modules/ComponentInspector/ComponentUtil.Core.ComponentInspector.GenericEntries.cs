using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using RSkoi_ComponentUtil.UI;

namespace RSkoi_ComponentUtil
{
    public partial class ComponentUtil
    {
        private void ConfigDropdown(
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
            Dropdown dropdownField = entry.GetComponentInChildren<Dropdown>();

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
                    dropdownField.onValueChanged.AddListener(value => SetPropertyValueInt(p, value, input));
                else
                    dropdownField.onValueChanged.AddListener(value => SetFieldValueInt(f, value, input));
                dropdownField.onValueChanged.AddListener(_ =>
                {
                    uiEntry.SetBgColorEdited();
                    ComponentUtilUI.TraverseAndSetEditedParents();
                });
            }
            registerMyEvents();

            uiEntry.UiComponentSetValueResetDelegate = (value) =>
            {
                // suspending all (non-persistent) listeners because inputField.text
                // will trigger onValueChanged, which would add to tracker via setter
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
            Component input,
            PropertyInfo p,
            FieldInfo f,
            Type type,
            bool setMethodIsPublic,
            bool isProperty,
            object value)
        {
            Toggle toggleField = entry.GetComponentInChildren<Toggle>();
            toggleField.isOn = (bool)value;
            toggleField.interactable = setMethodIsPublic;

            void registerMyEvents()
            {
                if (isProperty)
                    toggleField.onValueChanged.AddListener(value => SetPropertyValue(p, value.ToString(), input));
                else
                    toggleField.onValueChanged.AddListener(value => SetFieldValue(f, value.ToString(), input));
                toggleField.onValueChanged.AddListener(_ =>
                {
                    uiEntry.SetBgColorEdited();
                    ComponentUtilUI.TraverseAndSetEditedParents();
                });
            }
            registerMyEvents();

            uiEntry.UiComponentSetValueResetDelegate = (value) =>
            {
                // suspending all (non-persistent) listeners because inputField.text
                // will trigger onValueChanged, which would add to tracker via setter
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
            Component input,
            PropertyInfo p,
            FieldInfo f,
            Type type,
            bool setMethodIsPublic,
            bool isProperty,
            object value)
        {
            InputField inputField = entry.GetComponentInChildren<InputField>();
            inputField.text = value.ToString();
            // trying to cram an integer into (e.g.) a short could lead to problems
            inputField.contentType =
                TypeIsFloatingPoint(type) ? InputField.ContentType.DecimalNumber : InputField.ContentType.IntegerNumber;
            inputField.interactable = setMethodIsPublic;

            void registerMyEvents()
            {
                if (isProperty)
                    inputField.onValueChanged.AddListener(value => SetPropertyValue(p, value, input));
                else
                    inputField.onValueChanged.AddListener(value => SetFieldValue(f, value, input));
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
                // will trigger onValueChanged, which would add to tracker via setter
                inputField.onValueChanged.RemoveAllListeners();
                inputField.text = value.ToString();
                registerMyEvents();
                return inputField.text;
            };

            uiEntry.ParentUiEntry = parentUiEntry;
        }

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

        private bool TypeIsFloatingPoint(Type type)
        {
            if (type.Equals(typeof(float))
                || type.Equals(typeof(double))
                || type.Equals(typeof(decimal)))
                return true;
            return false;
        }
    }
}
