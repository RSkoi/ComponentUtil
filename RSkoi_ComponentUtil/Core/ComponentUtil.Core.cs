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

        private static readonly List<Type> _supportedTypes =
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

            ClearEntryList(ComponentUtilUI._transformListEntries);
            ClearEntryList(ComponentUtilUI._componentListEntries);
            ClearEntryList(ComponentUtilUI._componentPropertyListEntries);
            ClearEntryList(ComponentUtilUI._componentFieldListEntries);
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

            ClearEntryList(ComponentUtilUI._transformListEntries);

            Transform[] list = input.GetComponentsInChildren<Transform>();
            foreach (Transform t in list)
            {
                GameObject go = Instantiate(ComponentUtilUI._genericListEntryPrefab, ComponentUtilUI._transformListContainer);
                go.transform.Find("EntryLabel").GetComponent<Text>().text = t.name;
                go.GetComponent<Button>().onClick.AddListener(() => ChangeSelectedGO(t.gameObject));

                ComponentUtilUI._transformListEntries.Add(go, t);
            }
            _selectedGO = list[0].gameObject;
        }

        /// <summary>
        /// get all components on currently selected transform, map to list entries
        /// selects first list entry
        /// </summary>
        /// <param name="input">selected object to find all components in</param>
        public void GetAllComponents(GameObject input)
        {
            if (input == null)
                return;

            ClearEntryList(ComponentUtilUI._componentListEntries);

            UpdateUISelectedText(ComponentUtilUI._componentListSelectedGOText, input.name);

            Component[] list = input.GetComponents(typeof(Component));
            foreach (Component c in list)
            {
                GameObject go = Instantiate(ComponentUtilUI._genericListEntryPrefab, ComponentUtilUI._componentListContainer);
                go.transform.Find("EntryLabel").GetComponent<Text>().text = c.GetType().Name;
                go.GetComponent<Button>().onClick.AddListener(() => ChangeSelectedComponent(c));

                ComponentUtilUI._componentListEntries.Add(go, c);
            }
            _selectedComponent = list[0];
        }

        /// <summary>
        /// get all fields of currently selected component, map to list entries of different types
        /// </summary>
        /// <param name="input">selected component to list the properties and fields of</param>
        public void GetAllFieldsAndProperties(Component input)
        {
            if (input == null)
                return;

            ClearEntryList(ComponentUtilUI._componentPropertyListEntries);
            ClearEntryList(ComponentUtilUI._componentFieldListEntries);

            UpdateUISelectedText(ComponentUtilUI._componentPropertyListSelectedComponentText, input.gameObject.name + "." + input.GetType().Name);

            foreach (PropertyInfo p in input
                .GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                // reference types are annoying
                // this includes strings
                if (!p.PropertyType.IsValueType)
                    continue;

                // some getters are not public?
                if (p.GetGetMethod() == null)
                    continue;

                GameObject entry = ConfigureComponentEntry(input, p, null);
                if (entry == null)
                    continue;
                ComponentUtilUI._componentPropertyListEntries.Add(entry, p);
            }

            foreach (FieldInfo f in input
                .GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                // reference types are annoying
                // this includes strings
                if (!f.FieldType.IsValueType)
                    continue;

                GameObject entry = ConfigureComponentEntry(input, null, f);
                if (entry == null)
                    continue;
                ComponentUtilUI._componentFieldListEntries.Add(entry, f);
            }
        }

        #region private
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

        private void UpdateUISelectedText(Text uiText, string selectedName, char separator = ':')
        {
            int splitIndex = uiText.text.IndexOf(separator);
            string newText = uiText.text.Substring(0, splitIndex + 1);
            newText += " <b>" + selectedName + "</b>";
            uiText.text = newText;
        }

        private void ClearEntryList<T>(Dictionary<GameObject, T> list)
        {
            // destroying UI objects is really bad for performance
            // TODO: implement pooling, remember to remove onClick listeners
            foreach (var t in list)
                Destroy(t.Key);
            list.Clear();
        }

        private GameObject MapPropertyOrFieldToEntryPrefab(Type t)
        {
            if (t.IsEnum)
                return ComponentUtilUI._componentPropertyEnumEntryPrefab;
            else if (t.Equals(typeof(bool)))
                return ComponentUtilUI._componentPropertyBoolEntryPrefab;

            return ComponentUtilUI._componentPropertyDecimalEntryPrefab;
        }

        private GameObject ConfigureComponentEntry(Component input, PropertyInfo p, FieldInfo f)
        {
            bool isProperty = p != null;
            bool isField = f != null;
            // this should never be the case
            if (!isProperty && !isField)
                return null;

            Type type = isProperty ? p.PropertyType : f.FieldType;
            if (!type.IsEnum && !_supportedTypes.Contains(type))
                return null;

            // properties without set method will be non-interactable
            bool setMethodIsPublic = !isProperty || (p.GetSetMethod() != null);

            GameObject entry = Instantiate(
                    MapPropertyOrFieldToEntryPrefab(type),
                    ComponentUtilUI._componentPropertyListContainer);
            entry.transform.Find("EntryLabel").GetComponent<Text>().text
                = isProperty ? $"[Property] {p.Name}" : $"[Field] {f.Name}";

            object value = GetValueFieldOrProperty(input, p, f);
            if (value == null)
                return null;

            if (type.IsEnum)
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
                if (isProperty)
                    dropdownField.onValueChanged.AddListener(value => SetPropertyValueInt(p, value, input));
                else
                    dropdownField.onValueChanged.AddListener(value => SetFieldValueInt(f, value, input));
            }
            else if (type.Equals(typeof(bool)))
            {
                Toggle toggleField = entry.GetComponentInChildren<Toggle>();
                toggleField.isOn = (bool)value;
                toggleField.interactable = setMethodIsPublic;

                if (isProperty)
                    toggleField.onValueChanged.AddListener(value => SetPropertyValue(p, value.ToString(), input));
                else
                    toggleField.onValueChanged.AddListener(value => SetFieldValue(f, value.ToString(), input));
            }
            else if (type.Equals(typeof(Vector2)) || type.Equals(typeof(Vector3)) || type.Equals(typeof(Vector4)))
            {
                // TODO: how to handle Vectors?
                Destroy(entry);
                return null;
            }
            else if (type.Equals(typeof(Quaternion)))
            {
                // TODO: how to handle Quaternions? probably same way as Vector4
                Destroy(entry);
                return null;
            }
            else if (type.Equals(typeof(Color)))
            {
                // TODO: color picker for UnityEngine.Color type
                Destroy(entry);
                return null;
            }
            else
            {
                // default is decimal input field
                InputField inputField = entry.GetComponentInChildren<InputField>();
                inputField.text = value.ToString();
                // trying to cram an integer into (e.g.) a short could lead to problems
                inputField.contentType =
                    TypeIsFloatingPoint(type) ? InputField.ContentType.DecimalNumber : InputField.ContentType.IntegerNumber;
                inputField.interactable = setMethodIsPublic;

                if (isProperty)
                    inputField.onValueChanged.AddListener(value => SetPropertyValue(p, value, input));
                else
                    inputField.onValueChanged.AddListener(value => SetFieldValue(f, value, input));
            }

            return entry;
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
                    _instance.AddToTracker(_selectedObject, input.gameObject, input, p.Name, p.GetValue(input, null),
                        PropertyTrackerData.Options.IsProperty);

                p.SetValue(input, Convert.ChangeType(value, p.PropertyType), null);
            }
            catch (Exception e) { logger.LogError(e); }
        }

        internal void SetPropertyValueInt(PropertyInfo p, int value, Component input, bool track = true)
        {
            try
            {
                if (track)
                    _instance.AddToTracker(_selectedObject, input.gameObject, input, p.Name, (int)p.GetValue(input, null),
                        PropertyTrackerData.Options.IsProperty | PropertyTrackerData.Options.IsInt);
                    
                p.SetValue(input, value, null);
            }
            catch (Exception e) { logger.LogError(e); }
        }

        internal void SetFieldValue(FieldInfo f, string value, Component input, bool track = true)
        {
            try
            {
                if (track)
                    _instance.AddToTracker(_selectedObject, input.gameObject, input, f.Name, f.GetValue(input),
                        PropertyTrackerData.Options.None);
                
                f.SetValue(input, Convert.ChangeType(value, f.FieldType));
            }
            catch (Exception e) { logger.LogError(e); }
        }

        internal void SetFieldValueInt(FieldInfo f, int value, Component input, bool track = true)
        {
            try
            {
                if (track)
                    _instance.AddToTracker( _selectedObject, input.gameObject, input, f.Name, (int)f.GetValue(input),
                        PropertyTrackerData.Options.IsInt);
                
                f.SetValue(input, value);
                
            }
            catch (Exception e) { logger.LogError(e); }
        }
        #endregion internal setters
    }
}
