using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using RSkoi_ComponentUtil.UI;
using RSkoi_ComponentUtil.Core;

namespace RSkoi_ComponentUtil
{
    public partial class ComponentUtil
    {
        #region currently selected
        internal static Studio.ObjectCtrlInfo _selectedObject;
        private static GameObject _selectedGO;
        private static Component _selectedComponent;

        private static ComponentUtilUI.GenericUIListEntry _selectedTransformUIEntry;
        private static ComponentUtilUI.GenericUIListEntry _selectedComponentUiEntry;
        #endregion currently selected

        #region current page
        internal static int _currentPageTransformList = 0;
        internal static int _currentPageComponentList = 0;
        #endregion current page

        /// <summary>
        /// the types ComponentUtil supports
        /// </summary>
        public static readonly HashSet<Type> supportedTypes =
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
        /// sets selected objects to null, resets the tracker, UI pools, cache and pages
        /// </summary>
        public void ResetState()
        {
            _selectedGO = null;
            _selectedComponent = null;
            _selectedObject = null;

            _currentPageTransformList = 0;
            _currentPageComponentList = 0;

            ClearTracker();
            ComponentUtilUI.ResetPageNumbers();
            ComponentUtilUI.ClearAllEntryPools();
            ComponentUtilCache.ClearCache();
        }

        /// <summary>
        /// entry point for the core functionality
        /// , flattens transform hierarchy, lists all components, lists all properties
        /// </summary>
        /// <param name="input">selected item/object to traverse</param>
        public void Entry(Studio.ObjectCtrlInfo input)
        {
            if (input == null)
                return;

            _currentPageTransformList = 0;
            ComponentUtilUI.ResetPageNumberTransform();

            _selectedObject = input;
            FlattenTransformHierarchy(input.guideObject.transformTarget.gameObject);
            GetAllComponents(_selectedGO, _selectedTransformUIEntry);
            GetAllFieldsAndProperties(_selectedComponent, _selectedComponentUiEntry);

            ComponentUtilUI.TraverseAndSetEditedParents();
        }

        #region internal
        /// <summary>
        /// flattens transform hierarchy of input to list entries
        /// , selects first list entry
        /// </summary>
        /// <param name="input">selected item/object to traverse</param>
        /// <param name="setsSelected">whether _selectedGO and _selectedTransformUIEntry
        /// should be set to first item in flattened transform hiararchy
        /// ; also whether to reset current component page number</param>
        internal void FlattenTransformHierarchy(GameObject input, bool setsSelected = true)
        {
            if (input == null)
                return;

            List<Transform> list = [ ..ComponentUtilCache.GetOrCacheTransforms(input) ];

            // filter string
            string filter = ComponentUtilUI.PageSearchTransformInputValue.ToLower();
            if (filter != "")
                list = list.Where(t => t.name.ToLower().Contains(filter)).ToList();

            // paging
            int itemsPerPage = ItemsPerPageValue;
            int startIndex = _currentPageTransformList * itemsPerPage;
            int n = (list.Count - startIndex) <= itemsPerPage ? list.Count - startIndex : itemsPerPage;
            if (list.Count != 0)
                list = list.GetRange(startIndex, n);

            ComponentUtilUI.PrepareTransformPool(list.Count);
            for (int poolIndex = 0; poolIndex < list.Count; poolIndex++)
            {
                Transform t = list[poolIndex];

                ComponentUtilUI.GenericUIListEntry uiEntry = ComponentUtilUI.TransformListEntries[poolIndex];
                uiEntry.EntryName.text = t.name;
                // remove all (non-persistent) listeners
                uiEntry.SelfButton.onClick.RemoveAllListeners();
                uiEntry.SelfButton.onClick.AddListener(() => ChangeSelectedGO(t.gameObject, uiEntry));
                // transform ui entries have no parent entry
                uiEntry.ParentUiEntry = null;
                uiEntry.UiTarget = t;
                uiEntry.ResetBgAndChildren();
                uiEntry.UiGO.SetActive(true);
            }

            SetSelectedTransform(setsSelected, list);
        }

        /// <summary>
        /// gets all components on currently selected transform, maps to list entries
        /// , selects first list entry
        /// </summary>
        /// <param name="input">selected object to find all components in</param>
        /// <param name="inputUi">selected transform ui entry</param>
        /// <param name="setsSelected">whether _selectedComponent and _selectedComponentUiEntry
        /// should be set to first item in component list</param>
        internal void GetAllComponents(GameObject input, ComponentUtilUI.GenericUIListEntry inputUi, bool setsSelected = true)
        {
            if (input == null || inputUi == null)
                return;

            ComponentUtilUI.UpdateUISelectedText(ComponentUtilUI._componentListSelectedGOText, input.name);

            List<Component> list = [ ..ComponentUtilCache.GetOrCacheComponents(input) ];

            // filter string
            string filter = ComponentUtilUI.PageSearchComponentInputValue.ToLower();
            if (filter != "")
                list = list.Where(c => c.GetType().Name.ToLower().Contains(filter)).ToList();

            // paging
            int itemsPerPage = ItemsPerPageValue;
            int startIndex = _currentPageComponentList * itemsPerPage;
            int n = (list.Count - startIndex) <= itemsPerPage ? list.Count - startIndex : itemsPerPage;

            if (list.Count != 0)
                list = list.GetRange(startIndex, n);
            
            ComponentUtilUI.PrepareComponentPool(list.Count);
            for (int poolIndex = 0; poolIndex < list.Count; poolIndex++)
            {
                Component c = list[poolIndex];

                ComponentUtilUI.GenericUIListEntry uiEntry = ComponentUtilUI.ComponentListEntries[poolIndex];
                uiEntry.EntryName.text = c.GetType().Name;
                // remove all (non-persistent) listeners
                uiEntry.SelfButton.onClick.RemoveAllListeners();
                uiEntry.SelfButton.onClick.AddListener(() => ChangeSelectedComponent(c, uiEntry));
                uiEntry.ParentUiEntry = inputUi;
                uiEntry.UiTarget = c;
                uiEntry.ResetBgAndChildren();
                uiEntry.UiGO.SetActive(true);
            }

            SetSelectedComponent(setsSelected, list);
        }

        /// <summary>
        /// gets all fields of currently selected component, maps to list entries of different types
        /// </summary>
        /// <param name="input">selected component to list the properties and fields of</param>
        /// <param name="inputUi">selected component ui entry</param>
        internal void GetAllFieldsAndProperties(Component input, ComponentUtilUI.GenericUIListEntry inputUi)
        {
            if (input == null || inputUi == null)
                return;

            // destroying UI objects is really bad for performance
            // TODO: implement pooling for property/field elements, remember to remove onClick listeners
            ComponentUtilUI.ClearEntryListGO(ComponentUtilUI._componentPropertyListEntries);
            ComponentUtilUI.ClearEntryListGO(ComponentUtilUI._componentFieldListEntries);

            ComponentUtilUI.UpdateUISelectedText(
                ComponentUtilUI._componentPropertyListSelectedComponentText,
                input.gameObject.name + "." + input.GetType().Name);

            foreach (PropertyInfo p in ComponentUtilCache.GetOrCachePropertyInfos(input))
            {
                // reference types are annoying
                // this includes strings
                if (!p.PropertyType.IsValueType)
                    continue;

                // ignore properties with private getters
                if (p.GetGetMethod() == null)
                    continue;

                ConfigureComponentEntry(_selectedComponentUiEntry, input, p, null);
            }

            foreach (FieldInfo f in ComponentUtilCache.GetOrCacheFieldInfos(input))
            {
                // reference types are annoying
                // this includes strings
                if (!f.FieldType.IsValueType)
                    continue;

                ConfigureComponentEntry(_selectedComponentUiEntry, input, null, f);
            }
        }
        #endregion internal

        #region private
        private void ConfigureComponentEntry(ComponentUtilUI.GenericUIListEntry parentUiEntry, Component input, PropertyInfo p, FieldInfo f)
        {
            bool isProperty = p != null;
            bool isField = f != null;
            // this should never be the case
            if ((!isProperty && !isField) || (isProperty && isField))
            {
                logger.LogError("ConfigureComponentEntry: pass either PropertyInfo (X)OR FieldInfo as arguments");
                return;
            }
            
            Type type = isProperty ? p.PropertyType : f.FieldType;
            if (!type.IsEnum && !supportedTypes.Contains(type))
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
            {
                logger.LogWarning($"Could not find property or field on {input.transform.name}" +
                    $".{input.name} with name {propName}, destroying entry and returning");
                Destroy(entry);
                return;
            }

            if (type.IsEnum)
                ConfigDropdown(parentUiEntry, entry, uiEntry, input, p, f, type, setMethodIsPublic, isProperty, value);
            else if (type.Equals(typeof(bool)))
                ConfigToggle(parentUiEntry, entry, uiEntry, input, p, f, type, setMethodIsPublic, isProperty, value);
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
                ConfigInput(parentUiEntry, entry, uiEntry, input, p, f, type, setMethodIsPublic, isProperty, value);

            uiEntry.ResetBgAndChildren();

            if (isProperty)
                ComponentUtilUI._componentPropertyListEntries.Add(uiEntry);
            else
                ComponentUtilUI._componentFieldListEntries.Add(uiEntry);

            PropertyKey key = new(_selectedObject, input.gameObject, input);
            // make bg green if value is edited
            CheckTrackedAndMarkAsEdited(key, uiEntry, propName, value);

            ConfigReset(key, uiEntry, input, p, f, propName, setMethodIsPublic, isProperty);
        }

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
                dropdownField.onValueChanged.AddListener(_ => uiEntry.SetBgColorEdited());
                dropdownField.onValueChanged.AddListener(_ => ComponentUtilUI.TraverseAndSetEditedParents());
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
                toggleField.onValueChanged.AddListener(_ => uiEntry.SetBgColorEdited());
                toggleField.onValueChanged.AddListener(_ => ComponentUtilUI.TraverseAndSetEditedParents());
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
                inputField.onValueChanged.AddListener(_ => uiEntry.SetBgColorEdited());
                inputField.onValueChanged.AddListener(_ => ComponentUtilUI.TraverseAndSetEditedParents());
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
                if (isProperty)
                    p.SetValue(input, defaultValue, null);
                else
                    f.SetValue(input, defaultValue);

                RemovePropertyFromTracker(key, propName);
                uiEntry.SetUiComponentTargetValue(defaultValue);
                uiEntry.SetBgColorDefault();
                ComponentUtilUI.TraverseAndSetEditedParents();
            });
        }

        private void CheckTrackedAndMarkAsEdited(
            PropertyKey key,
            ComponentUtilUI.PropertyUIEntry uiEntry,
            string propName,
            object value)
        {
            if (!PropertyIsTracked(key, propName))
                return;
            
            object defaultValue = GetTrackedDefaultValue(key, propName);
            if (defaultValue == null)
            {
                logger.LogError($"CheckTrackedAndMarkAsEdited: no defaultValue for property {propName} found or value is null");
                return;
            }

            if (defaultValue.ToString() != value.ToString())
                uiEntry.SetBgColorEdited();
        }

        private void ChangeSelectedGO(GameObject target, ComponentUtilUI.GenericUIListEntry uiEntry)
        {
            _selectedGO = target;
            _selectedTransformUIEntry = uiEntry;

            _currentPageComponentList = 0;
            ComponentUtilUI.ResetPageNumberComponent();

            GetAllComponents(_selectedGO, _selectedTransformUIEntry);
            GetAllFieldsAndProperties(_selectedComponent, _selectedComponentUiEntry);

            ComponentUtilUI.TraverseAndSetEditedParents();
        }

        private void ChangeSelectedComponent(Component target, ComponentUtilUI.GenericUIListEntry uiEntry)
        {
            _selectedComponent = target;
            _selectedComponentUiEntry = uiEntry;

            uiEntry.ResetBgAndChildren();

            GetAllFieldsAndProperties(_selectedComponent, _selectedComponentUiEntry);

            ComponentUtilUI.TraverseAndSetEditedParents();
        }

        private void SetSelectedTransform(bool setsSelected, List<Transform> cacheList)
        {
            if (!setsSelected)
                return;
            if (cacheList == null)
                return;

            _currentPageComponentList = 0;
            ComponentUtilUI.ResetPageNumberComponent();

            if (cacheList.Count == 0)
                return;

            _selectedGO = cacheList[0].gameObject;
            _selectedTransformUIEntry = ComponentUtilUI.TransformListEntries[0];
        }

        private void SetSelectedComponent(bool setsSelected, List<Component> cacheList)
        {
            if (!setsSelected)
                return;
            if (cacheList == null)
                return;

            if (cacheList.Count == 0)
                return;

            _selectedComponent = cacheList[0];
            _selectedComponentUiEntry = ComponentUtilUI.ComponentListEntries[0];
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
            logger.LogError("GetValueFieldOrProperty received neither PropertyInfo nor FieldInfo");
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
        }
        #endregion internal setters

        #region internal page operations
        internal void OnLastTransformPage()
        {
            if (_selectedObject == null)
                return;
            if (_currentPageTransformList == 0)
                return;

            _currentPageTransformList--;
            ComponentUtilUI.UpdatePageNumberTransform(_currentPageTransformList);
            FlattenTransformHierarchy(_selectedObject.guideObject.transformTarget.gameObject, false);
            ComponentUtilUI.TraverseAndSetEditedParents();
        }

        internal void OnNextTransformPage()
        {
            if (_selectedObject == null)
                return;
            int toBeStartIndex = (_currentPageTransformList + 1) * ItemsPerPageValue;
            // page switch can only occur after the _selectedObject has been scanned for transforms
            Transform[] cached = ComponentUtilCache._transformSearchCache[_selectedObject.guideObject.transformTarget.gameObject];
            // out of bounds start index
            if (toBeStartIndex >= cached.Length)
                return;

            // if filter string reduces length of transform list
            string filter = ComponentUtilUI.PageSearchTransformInputValue.ToLower();
            if (filter != "" && (toBeStartIndex >= cached
                .Where(t => t.name.ToLower().Contains(filter))
                .ToArray().Length))
                return;

            _currentPageTransformList++;
            ComponentUtilUI.UpdatePageNumberTransform(_currentPageTransformList);
            FlattenTransformHierarchy(_selectedObject.guideObject.transformTarget.gameObject, false);
            ComponentUtilUI.TraverseAndSetEditedParents();
        }

        internal void OnLastComponentPage()
        {
            if (_selectedGO == null)
                return;
            if (_currentPageComponentList == 0)
                return;

            _currentPageComponentList--;
            ComponentUtilUI.UpdatePageNumberComponent(_currentPageComponentList);
            GetAllComponents(_selectedGO, _selectedTransformUIEntry, false);
            ComponentUtilUI.TraverseAndSetEditedParents();
        }

        internal void OnNextComponentPage()
        {
            if (_selectedGO == null)
                return;
            int toBeStartIndex = (_currentPageComponentList + 1) * ItemsPerPageValue;
            // page switch can only occur after the _selectedGO has been scanned for components
            Component[] cached = ComponentUtilCache._componentSearchCache[_selectedGO];
            // out of bounds start index
            if (toBeStartIndex >= cached.Length)
                return;

            // if filter string reduces length of transform list
            string filter = ComponentUtilUI.PageSearchComponentInputValue.ToLower();
            if (filter != "" && (toBeStartIndex >= cached
                .Where(c => c.GetType().Name.ToLower().Contains(filter))
                .ToArray().Length))
                return;

            _currentPageComponentList++;
            ComponentUtilUI.UpdatePageNumberComponent(_currentPageComponentList);
            GetAllComponents(_selectedGO, _selectedTransformUIEntry, false);
            ComponentUtilUI.TraverseAndSetEditedParents();
        }
        #endregion internal page operations

        #region internal search operations
        internal void OnFilterTransform()
        {
            if (_selectedObject == null)
                return;

            // essentially does this:
            //Entry(_selectedObject);

            _currentPageTransformList = 0;
            ComponentUtilUI.UpdatePageNumberTransform(_currentPageTransformList);

            FlattenTransformHierarchy(_selectedObject.guideObject.transformTarget.gameObject);
            GetAllComponents(_selectedGO, _selectedTransformUIEntry);
            GetAllFieldsAndProperties(_selectedComponent, _selectedComponentUiEntry);

            ComponentUtilUI.TraverseAndSetEditedParents();
        }

        internal void OnFilterComponent()
        {
            if (_selectedGO == null)
                return;

            _currentPageComponentList = 0;
            ComponentUtilUI.ResetPageNumberComponent();

            GetAllComponents(_selectedGO, _selectedTransformUIEntry);
            GetAllFieldsAndProperties(_selectedComponent, _selectedComponentUiEntry);

            ComponentUtilUI.TraverseAndSetEditedParents();
        }
        #endregion internal search operations
    }
}
