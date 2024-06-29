using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

using RSkoi_ComponentUtil.UI;
using RSkoi_ComponentUtil.Core;

namespace RSkoi_ComponentUtil
{
    public partial class ComponentUtil
    {
        /// <summary>
        /// sets up component inspector: configure delete component button, 
        /// gets all fields of currently selected component, maps to list entries of different types
        /// </summary>
        /// <param name="input">selected component to list the properties and fields of</param>
        /// <param name="inputUi">selected component ui entry</param>
        internal void GetAllFieldsAndProperties(Component input, ComponentUtilUI.GenericUIListEntry inputUi)
        {
            if (input == null || inputUi == null)
                return;

            #region component delete button
            // delete component button is only interactable if the component is tracked
            ComponentUtilUI._componentDeleteButton.interactable = false;
            ComponentUtilUI._componentDeleteButton.onClick.RemoveAllListeners();
            if (ComponentIsTracked(_selectedObject, input.gameObject, input.GetType().FullName))
            {
                ComponentUtilUI._componentDeleteButton.interactable = true;
                ComponentUtilUI._componentDeleteButton.onClick.AddListener(() =>
                {
                    GameObject go = input.gameObject;
                    if (RemoveComponentFromTracker(_selectedObject, go, input))
                        DestroyImmediate(input); // scary, so that GetOrCacheComponents doesn't pick it up again

                    // force refresh cache
                    ComponentUtilCache.ClearComponentFromCache(input);
                    ComponentUtilCache.GetOrCacheComponents(go, true);

                    Entry(_selectedObject);
                });
            }
            #endregion component delete button

            // destroying UI objects is really bad for performance
            // TODO: implement pooling for property/field elements, remember to remove onClick listeners
            ComponentUtilUI.ClearInspectorEntryPools();

            ComponentUtilUI.UpdateUISelectedText(
                ComponentUtilUI._componentPropertyListSelectedComponentText,
                $"{input.gameObject.name}.{input.GetType().Name}");
            ComponentUtilUI.UpdateUISelectedText(
                ComponentUtilUI._objectPropertyListSelectedText,
                "None");
            _selectedReferencePropertyUiEntry = null;

            foreach (PropertyInfo p in ComponentUtilCache.GetOrCachePropertyInfos(input))
            {
                // TODO: strings

                // reference types are annoying
                // this includes strings
                //if (!p.PropertyType.IsValueType)
                //    continue;

                // indexed properties are annoying
                if (p.GetIndexParameters().Length != 0)
                    continue;

                // ignore properties with private getters
                if (p.GetGetMethod() == null)
                    continue;

                ConfigurePropertyEntry(_selectedComponentUiEntry, input, p, null);
            }

            foreach (FieldInfo f in ComponentUtilCache.GetOrCacheFieldInfos(input))
            {
                // reference types are annoying
                // this includes strings
                //if (!f.FieldType.IsValueType)
                //    continue;

                ConfigurePropertyEntry(_selectedComponentUiEntry, input, null, f);
            }
        }

        internal object GetValueFieldOrProperty(object input, PropertyInfo p, FieldInfo f)
        {
            bool isProperty = p != null;
            bool isField = f != null;

            if (isProperty)
                return p.GetValue(input, null);
            if (isField)
                return f.GetValue(input);

            // this should never be the case
            _logger.LogError("GetValueFieldOrProperty received neither PropertyInfo nor FieldInfo");
            return null;
        }

        private void ConfigurePropertyEntry(ComponentUtilUI.GenericUIListEntry parentUiEntry, object input, PropertyInfo p, FieldInfo f, bool objectMode = false)
        {
            // objectMode signifies whether we're populating ObjectInspector (true) or ComponentInspector (false)
            // input is either
            //      - the selected Component, or
            //      - a referenced object to list the properties and fields of
            // cInput is input cast to Component if input is selected Component
            // value is either
            //      - the value of the property / field on the Component, or
            //      - the value of the property / field on the reference type

            Component cInput = objectMode ? null : (Component)input;
            if (!objectMode && cInput == null)
            {
                _logger.LogError("ConfigurePropertyEntry: objectMode is false, but could not convert input to Component");
                return;
            }

            bool isProperty = p != null;
            bool isField = f != null;
            // this should never be the case
            if ((!isProperty && !isField) || (isProperty && isField))
            {
                _logger.LogError("ConfigurePropertyEntry: pass either PropertyInfo (X)OR FieldInfo as arguments");
                return;
            }

            Type type = isProperty ? p.PropertyType : f.FieldType;
            bool typeIsSupported = supportedTypes.Contains(type);
            bool typeIsSupportedAndRewired = supportedTypesRewireAsReference.Contains(type);
            if (!type.IsEnum && !typeIsSupported && !typeIsSupportedAndRewired && type.IsValueType)
                return;

            // properties without set method will be non-interactable except 'generic' reference types
            bool setMethodIsPublic = !isProperty || (p.GetSetMethod() != null);
            string propName = isProperty ? p.Name : f.Name;

            GameObject entryPrefab = ComponentUtilUI.MapPropertyOrFieldToEntryPrefab(type);
            Transform propertyListContainer = objectMode
                ? ComponentUtilUI._objectPropertyListContainer : ComponentUtilUI._componentPropertyListContainer;
            GameObject entry = Instantiate(entryPrefab, propertyListContainer);
            ComponentUtilUI.PropertyUIEntry uiEntry = ComponentUtilUI.PreConfigureNewUiEntry(entry, entryPrefab);
            uiEntry.PropertyName.text = isProperty ? $"[Property] {propName}" : $"[Field] {propName}";

            object value = GetValueFieldOrProperty(objectMode ? input : cInput, p, f);
            if (value == null)
            {
                _logger.LogWarning($"Value of property or field on {cInput.transform.name}" +
                    $".{cInput.GetType().Name} with name {propName} is null, destroying entry and returning");
                Destroy(entry);
                return;
            }

            bool isValidReference = !objectMode                         // don't recurse down from object inspector
                && !typeIsSupported                                     // don't override types with custom property entries
                && (!type.IsValueType || typeIsSupportedAndRewired);    // is reference type or rewired
            if (isValidReference)
                ConfigReference(parentUiEntry, entry, uiEntry, cInput, p, f, type, setMethodIsPublic, isProperty, value);
            else if (type.IsEnum)
                ConfigDropdown(parentUiEntry, entry, uiEntry, objectMode ? input : cInput, p, f, type, setMethodIsPublic, isProperty, value, objectMode);
            else if (type.Equals(typeof(bool)))
                ConfigToggle(parentUiEntry, entry, uiEntry, objectMode ? input : cInput, p, f, type, setMethodIsPublic, isProperty, value, objectMode);
            else if (type.Equals(typeof(Vector2)) || type.Equals(typeof(Vector3))
                  || type.Equals(typeof(Vector4)) || type.Equals(typeof(Quaternion)))
            {
                if (!ConfigVector(parentUiEntry, entry, uiEntry, objectMode ? input : cInput, p, f, type, setMethodIsPublic, isProperty, value, objectMode))
                {
                    _logger.LogWarning($"Could not configure vector property entry on {cInput.transform.name}" +
                    $".{cInput.name} with name {propName}, destroying entry and returning");
                    Destroy(entry);
                    return;
                }
            }
            else if (type.Equals(typeof(Color)))
            {
                // TODO: color picker for UnityEngine.Color type
                Destroy(entry);
                return;
            }
            else if (type.Equals(typeof(AnimationCurve)))
            {
                // TODO: spline editor for UnityEngine.AnimationCurve type
                Destroy(entry);
                return;
            }
            else // default is decimal input field
                ConfigInput(parentUiEntry, entry, uiEntry, objectMode ? input : cInput, p, f, type, setMethodIsPublic, isProperty, value, objectMode);

            uiEntry.ResetBgAndChildren();

            List<ComponentUtilUI.PropertyUIEntry> uiCache = objectMode
                ? (isProperty ? ComponentUtilUI._objectPropertyListEntries : ComponentUtilUI._objectFieldListEntries)
                : (isProperty ? ComponentUtilUI._componentPropertyListEntries : ComponentUtilUI._componentFieldListEntries);
            uiCache.Add(uiEntry);
            
            if (isValidReference) // reference type
            {
                PropertyKey key = new(_selectedObject, cInput.gameObject, cInput);
                // make green if dummy property entry is tracked
                CheckTrackedAndMarkAsEditedReference(key, uiEntry, propName);

                //PropertyReferenceKey keyRef = new(_selectedObject, cInput.gameObject, cInput, propName);
                ConfigResetReference(null, uiEntry, input, p, f, propName, isProperty);
            }
            else if (objectMode) // property inside reference type / the object inspector
            {
                Component c = (Component)parentUiEntry.UiTarget;
                PropertyReferenceKey key = new(_selectedObject, c.gameObject, c, _selectedReferencePropertyUiEntry.PropertyNameValue);
                // make bg green if value is edited
                CheckTrackedAndMarkAsEdited(key, uiEntry, propName, value);

                ConfigResetReferenceProperty(key, uiEntry, input, p, f, propName, setMethodIsPublic, isProperty);
            }
            else // property in the component inspector
            {
                PropertyKey key = new(_selectedObject, cInput.gameObject, cInput);
                // make bg green if value is edited
                CheckTrackedAndMarkAsEdited(key, uiEntry, propName, value);

                ConfigReset(key, uiEntry, cInput, p, f, propName, setMethodIsPublic, isProperty);
            }
        }

        private void CheckTrackedAndMarkAsEdited(
            PropertyReferenceKey key,
            ComponentUtilUI.PropertyUIEntry uiEntry,
            string propName,
            object value)
        {
            if (!PropertyIsTracked(key, propName))
                return;

            object defaultValue = GetTrackedDefaultValue(key, propName);
            if (defaultValue == null)
            {
                _logger.LogError($"CheckTrackedAndMarkAsEdited: no defaultValue for property {propName} found or value is null");
                return;
            }

            if (defaultValue.ToString() != value.ToString())
                uiEntry.SetBgColorEdited();
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
                _logger.LogError($"CheckTrackedAndMarkAsEdited: no defaultValue for property {propName} found or value is null");
                return;
            }

            if (defaultValue.ToString() != value.ToString())
                uiEntry.SetBgColorEdited();
        }

        private void CheckTrackedAndMarkAsEditedReference(
            PropertyKey key,
            ComponentUtilUI.PropertyUIEntry uiEntry,
            string propName)
        {
            if (!PropertyIsTracked(key, propName))
                return;

            uiEntry.SetBgColorEdited();
        }

        #region internal setters
        internal void SetPropertyValue(PropertyInfo p, string value, object input)
        {
            try
            {
                p.SetValue(input, Convert.ChangeType(value, p.PropertyType), null);
            }
            catch (Exception e) { _logger.LogError(e); }
        }

        internal void SetPropertyValueInt(PropertyInfo p, int value, object input)
        {
            try
            {
                p.SetValue(input, value, null);
            }
            catch (Exception e) { _logger.LogError(e); }
        }

        internal void SetFieldValue(FieldInfo f, string value, object input)
        {
            try
            {
                f.SetValue(input, Convert.ChangeType(value, f.FieldType));
            }
            catch (Exception e) { _logger.LogError(e); }
        }

        internal void SetFieldValueInt(FieldInfo f, int value, object input)
        {
            try
            {
                f.SetValue(input, value);
            }
            catch (Exception e) { _logger.LogError(e); }
        }
        #endregion internal setters
    }
}
