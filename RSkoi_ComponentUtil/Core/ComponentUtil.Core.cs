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
        #region currently selected
        private static GameObject _selectedGO;
        private static Component _selectedComponent;
        private static Studio.ObjectCtrlInfo _selectedObject;
        #endregion currently selected

        /// <summary>
        /// the types ComponentUtil supports
        /// </summary>

        public static readonly List<Type> _supportedTypes =
        [
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(bool),
            typeof(int),
            typeof(uint),
            typeof(short),
            typeof(ushort),
            typeof(long),
            typeof(ulong),
            typeof(byte),
            typeof(sbyte),
            typeof(nint),
            typeof(nuint),
        ];

        /// <summary>
        /// resets the UI and cached selected objects
        /// </summary>
        public void Reset()
        {
            _selectedGO = null;
            _selectedComponent = null;
            _selectedObject = null;

            ComponentUtilUI.ClearAllEntryLists();
        }

        /// <summary>
        /// entry point for the core functionality
        /// flattens transform hierarchy, lists all components, lists all properties
        /// </summary>
        /// <param name="input">selected item/object to traverse</param>
        public void Entry(Studio.ObjectCtrlInfo input)
        {
            if (input == null)
                return;

            _selectedObject = input;
            FlattenTransformHierarchy(input.guideObject.transformTarget.gameObject);
            GetAllComponents(_selectedGO);
            GetAllFieldsAndProperties(_selectedComponent);
        }

        /// <summary>
        /// flatten transform hierarchy of input to list entries
        /// selects first list entry
        /// </summary>
        /// <param name="input">selected item/object to traverse</param>
        public void FlattenTransformHierarchy(GameObject input)
        {
            if (input == null)
                return;

            ComponentUtilUI.ClearEntryListData(ComponentUtilUI._transformListEntries);

            Transform[] list = input.GetComponentsInChildren<Transform>();
            foreach (Transform t in list)
            {
                GameObject go = Instantiate(ComponentUtilUI._genericListEntryPrefab, ComponentUtilUI._transformListContainer);
                ComponentUtilUI.GenericUIListEntry uiEntry = ComponentUtilUI.PreConfigureNewGenericUIListEntry(go);
                uiEntry.EntryName.text = t.name;
                uiEntry.SelfButton.onClick.AddListener(() => ChangeSelectedGO(t.gameObject));
                uiEntry.UiTarget = t;

                ComponentUtilUI._transformListEntries.Add(t, uiEntry);
            }
            _selectedGO = list[0].gameObject;
        }

        /// <summary>
        /// gets all components on currently selected transform, maps to list entries
        /// selects first list entry
        /// </summary>
        /// <param name="input">selected object to find all components in</param>
        public void GetAllComponents(GameObject input)
        {
            if (input == null)
                return;

            ComponentUtilUI.ClearEntryListData(ComponentUtilUI._componentListEntries);

            ComponentUtilUI.UpdateUISelectedText(ComponentUtilUI._componentListSelectedGOText, input.name);

            Component[] list = input.GetComponents(typeof(Component));
            foreach (Component c in list)
            {
                GameObject go = Instantiate(ComponentUtilUI._genericListEntryPrefab, ComponentUtilUI._componentListContainer);
                ComponentUtilUI.GenericUIListEntry uiEntry = ComponentUtilUI.PreConfigureNewGenericUIListEntry(go);
                uiEntry.EntryName.text = c.GetType().Name;
                uiEntry.SelfButton.onClick.AddListener(() => ChangeSelectedComponent(c));
                uiEntry.UiTarget = c;

                ComponentUtilUI._componentListEntries.Add(c, uiEntry);
            }
            _selectedComponent = list[0];
        }

        /// <summary>
        /// gets all fields of currently selected component, maps to list entries of different types
        /// </summary>
        /// <param name="input">selected component to list the properties and fields of</param>
        public void GetAllFieldsAndProperties(Component input)
        {
            if (input == null)
                return;

            ComponentUtilUI.ClearEntryListGO(ComponentUtilUI._componentPropertyListEntries);
            ComponentUtilUI.ClearEntryListGO(ComponentUtilUI._componentFieldListEntries);

            ComponentUtilUI.UpdateUISelectedText(
                ComponentUtilUI._componentPropertyListSelectedComponentText,
                input.gameObject.name + "." + input.GetType().Name);

            foreach (PropertyInfo p in input
                .GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                // reference types are annoying
                // this includes strings
                if (!p.PropertyType.IsValueType)
                    continue;

                // ignore properties with private getters
                if (p.GetGetMethod() == null)
                    continue;

                ConfigureComponentEntry(input, p, null);
            }

            foreach (FieldInfo f in input
                .GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                // reference types are annoying
                // this includes strings
                if (!f.FieldType.IsValueType)
                    continue;

                ConfigureComponentEntry(input, null, f);
            }

            ComponentUtilUI.UpdateTransformsAndComponentsBg(_tracker.Keys);
        }

        #region private
        private void ConfigureComponentEntry(Component input, PropertyInfo p, FieldInfo f)
        {
            bool isProperty = p != null;
            bool isField = f != null;
            // this should never be the case
            if ((!isProperty && !isField) || (isProperty && isField))
            {
                logger.LogError("ConfigureComponentEntry: use either PropertyInfo or FieldInfo");
                return;
            }
            
            Type type = isProperty ? p.PropertyType : f.FieldType;
            if (!type.IsEnum && !_supportedTypes.Contains(type))
                return;

            // properties without set method will be non-interactable
            bool setMethodIsPublic = !isProperty || (p.GetSetMethod() != null);
            string propName = isProperty ? p.Name : f.Name;

            GameObject entryPrefab = ComponentUtilUI.MapPropertyOrFieldToEntryPrefab(type);
            GameObject entry = Instantiate(entryPrefab, ComponentUtilUI._componentPropertyListContainer);
            ComponentUtilUI.PropertyUIEntry uiEntry = ComponentUtilUI.PreConfigureNewUiEntry(entry, entryPrefab);
            uiEntry.PropertyName.text = isProperty ? $"[Property] {propName}" : $"[Field] {propName}";

            object value = GetValueFieldOrProperty(input, p, f);
            if (value == null)
                return;

            if (type.IsEnum)
                ConfigDropdown(entry, uiEntry, input, p, f, type, setMethodIsPublic, isProperty, value);
            else if (type.Equals(typeof(bool)))
                ConfigToggle(entry, uiEntry, input, p, f, type, setMethodIsPublic, isProperty, value);
            else if (type.Equals(typeof(Vector2)) || type.Equals(typeof(Vector3)) || type.Equals(typeof(Vector4)))
            {
                // TODO: how to handle Vectors?
                Destroy(entry);
                return;
            }
            else if (type.Equals(typeof(Quaternion)))
            {
                // TODO: how to handle Quaternions? probably same way as Vector4
                Destroy(entry);
                return;
            }
            else if (type.Equals(typeof(Color)))
            {
                // TODO: color picker for UnityEngine.Color type
                Destroy(entry);
                return;
            }
            else // default is decimal input field
                ConfigInput(entry, uiEntry, input, p, f, type, setMethodIsPublic, isProperty, value);

            if (isProperty)
                ComponentUtilUI._componentPropertyListEntries.Add(entry, uiEntry);
            else
                ComponentUtilUI._componentFieldListEntries.Add(entry, uiEntry);

            PropertyKey key = new(_selectedObject, input.gameObject, input);
            // make bg green if value is edited
            IterateAndCheck(key, uiEntry, propName, value);

            ConfigReset(key, uiEntry, input, p, f, propName, setMethodIsPublic, isProperty);
        }

        private void ConfigDropdown(
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
                dropdownField.onValueChanged.AddListener(_ => uiEntry.SetBgColorEdited());
            }
            registerMyEvents();

            uiEntry.UiComponentSetValueDelegateForReset = (value) =>
            {
                // suspending all (non-persistent) listeners because inputField.text
                // will trigger onValueChanged, which would add to tracker via setter
                dropdownField.onValueChanged.RemoveAllListeners();
                dropdownField.value = (int)value;
                registerMyEvents();
                return dropdownField.value;
            };
            uiEntry.UiComponentTarget = dropdownField;
        }

        private void ConfigToggle(
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
                toggleField.onValueChanged.AddListener(_ => uiEntry.SetBgColorEdited());
            }
            registerMyEvents();

            uiEntry.UiComponentSetValueDelegateForReset = (value) =>
            {
                // suspending all (non-persistent) listeners because inputField.text
                // will trigger onValueChanged, which would add to tracker via setter
                toggleField.onValueChanged.RemoveAllListeners();
                toggleField.isOn = (bool)value;
                registerMyEvents();
                return toggleField.isOn;
            };
            uiEntry.UiComponentTarget = toggleField;
        }

        private void ConfigInput(
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
                inputField.onValueChanged.AddListener(_ => uiEntry.SetBgColorEdited());
            }
            registerMyEvents();

            uiEntry.UiComponentSetValueDelegateForReset = (value) =>
            {
                // suspending all (non-persistent) listeners because inputField.text
                // will trigger onValueChanged, which would add to tracker via setter
                inputField.onValueChanged.RemoveAllListeners();
                inputField.text = value.ToString();
                registerMyEvents();
                return inputField.text;
            };
            uiEntry.UiComponentTarget = inputField;
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
                if (isProperty)
                    p.SetValue(input, defaultValue, null);
                else
                    f.SetValue(input, defaultValue);

                bool removed = RemovePropertyFromTracker(key, propName);
                uiEntry.SetUiComponentTargetValue(defaultValue);
                uiEntry.ResetBgColor();

                ComponentUtilUI.UpdateTransformsAndComponentsBg(_tracker.Keys);
            });
        }

        private void IterateAndCheck(
            PropertyKey key,
            ComponentUtilUI.PropertyUIEntry uiEntry,
            string propName,
            object value)
        {
            if (PropertyIsTracked(key, propName))
            {
                object defaultValue = GetTrackedDefaultValue(key, propName);
                if (defaultValue.ToString() != value.ToString())
                    uiEntry.SetBgColorEdited();
            }
        }

        private void ChangeSelectedGO(GameObject target)
        {
            _selectedGO = target;
            GetAllComponents(_selectedGO);
            GetAllFieldsAndProperties(_selectedComponent);
        }

        private void ChangeSelectedComponent(Component target)
        {
            _selectedComponent = target;
            GetAllFieldsAndProperties(_selectedComponent);
        }
        #endregion private

        #region private helpers
        private bool TypeIsFloatingPoint(Type type)
        {
            if (type.Equals(typeof(float))
                || type.Equals(typeof(double))
                || type.Equals(typeof(decimal)))
                return true;
            return false;
        }
        #endregion private helpers

        #region internal getters
        internal object GetValueFieldOrProperty(Component input, PropertyInfo p, FieldInfo f)
        {
            bool isProperty = p != null;
            bool isField = f != null;

            if (isProperty)
                return p.GetValue(input, null);
            if (isField)
                return f.GetValue(input);
            // this should never be the case
            return null;
        }
        #endregion internal getters

        #region internal setters
        // TODO: this whole region is horrible and will become unwieldy with more supported types

        internal void SetPropertyValue(PropertyInfo p, string value, Component input, bool track = true)
        {
            try
            {
                if (track)
                    AddPropertyToTracker(_selectedObject, input.gameObject, input, p.Name, p.GetValue(input, null),
                        PropertyTrackerData.PropertyTrackerDataOptions.IsProperty);

                p.SetValue(input, Convert.ChangeType(value, p.PropertyType), null);
            }
            catch (Exception e) { logger.LogError(e); }

            ComponentUtilUI.UpdateTransformsAndComponentsBg(_tracker.Keys);
        }

        internal void SetPropertyValueInt(PropertyInfo p, int value, Component input, bool track = true)
        {
            try
            {
                if (track)
                    AddPropertyToTracker(_selectedObject, input.gameObject, input, p.Name, (int)p.GetValue(input, null),
                        PropertyTrackerData.PropertyTrackerDataOptions.IsProperty | PropertyTrackerData.PropertyTrackerDataOptions.IsInt);
                    
                p.SetValue(input, value, null);
            }
            catch (Exception e) { logger.LogError(e); }

            ComponentUtilUI.UpdateTransformsAndComponentsBg(_tracker.Keys);
        }

        internal void SetFieldValue(FieldInfo f, string value, Component input, bool track = true)
        {
            try
            {
                if (track)
                    AddPropertyToTracker(_selectedObject, input.gameObject, input, f.Name, f.GetValue(input),
                        PropertyTrackerData.PropertyTrackerDataOptions.None);
                
                f.SetValue(input, Convert.ChangeType(value, f.FieldType));
            }
            catch (Exception e) { logger.LogError(e); }

            ComponentUtilUI.UpdateTransformsAndComponentsBg(_tracker.Keys);
        }

        internal void SetFieldValueInt(FieldInfo f, int value, Component input, bool track = true)
        {
            try
            {
                if (track)
                    AddPropertyToTracker( _selectedObject, input.gameObject, input, f.Name, (int)f.GetValue(input),
                        PropertyTrackerData.PropertyTrackerDataOptions.IsInt);
                
                f.SetValue(input, value);
            }
            catch (Exception e) { logger.LogError(e); }

            ComponentUtilUI.UpdateTransformsAndComponentsBg(_tracker.Keys);
        }
        #endregion internal setters
    }
}
