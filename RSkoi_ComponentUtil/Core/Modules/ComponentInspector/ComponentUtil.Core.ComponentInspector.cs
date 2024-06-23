using System;
using System.Reflection;
using UnityEngine;

using RSkoi_ComponentUtil.UI;
using RSkoi_ComponentUtil.Core;

namespace RSkoi_ComponentUtil
{
    public partial class ComponentUtil
    {
        /// <summary>
        /// sets up inspector: configure delete component button, 
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

                ConfigurePropertyEntry(_selectedComponentUiEntry, input, p, null);
            }

            foreach (FieldInfo f in ComponentUtilCache.GetOrCacheFieldInfos(input))
            {
                // reference types are annoying
                // this includes strings
                if (!f.FieldType.IsValueType)
                    continue;

                ConfigurePropertyEntry(_selectedComponentUiEntry, input, null, f);
            }
        }

        internal object GetValueFieldOrProperty(Component input, PropertyInfo p, FieldInfo f)
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

        private void ConfigurePropertyEntry(ComponentUtilUI.GenericUIListEntry parentUiEntry, Component input, PropertyInfo p, FieldInfo f)
        {
            bool isProperty = p != null;
            bool isField = f != null;
            // this should never be the case
            if ((!isProperty && !isField) || (isProperty && isField))
            {
                _logger.LogError("ConfigurePropertyEntry: pass either PropertyInfo (X)OR FieldInfo as arguments");
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
                _logger.LogWarning($"Could not find property or field on {input.transform.name}" +
                    $".{input.name} with name {propName}, destroying entry and returning");
                Destroy(entry);
                return;
            }

            if (type.IsEnum)
                ConfigDropdown(parentUiEntry, entry, uiEntry, input, p, f, type, setMethodIsPublic, isProperty, value);
            else if (type.Equals(typeof(bool)))
                ConfigToggle(parentUiEntry, entry, uiEntry, input, p, f, type, setMethodIsPublic, isProperty, value);
            else if (type.Equals(typeof(Vector2)) || type.Equals(typeof(Vector3))
                  || type.Equals(typeof(Vector4)) || type.Equals(typeof(Quaternion)))
            {
                if (!ConfigVector(parentUiEntry, entry, uiEntry, input, p, f, type, setMethodIsPublic, isProperty, value))
                {
                    _logger.LogWarning($"Could not configure vector property entry on {input.transform.name}" +
                    $".{input.name} with name {propName}, destroying entry and returning");
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

        #region internal setters
        internal void SetPropertyValue(PropertyInfo p, string value, Component input, bool track = true)
        {
            try
            {
                if (track)
                    AddPropertyToTracker(_selectedObject, input.gameObject, input, p.Name, p.GetValue(input, null),
                        PropertyTrackerData.PropertyTrackerDataOptions.IsProperty);

                p.SetValue(input, Convert.ChangeType(value, p.PropertyType), null);
            }
            catch (Exception e) { _logger.LogError(e); }
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
            catch (Exception e) { _logger.LogError(e); }
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
            catch (Exception e) { _logger.LogError(e); }
        }

        internal void SetFieldValueInt(FieldInfo f, int value, Component input, bool track = true)
        {
            try
            {
                if (track)
                    AddPropertyToTracker(_selectedObject, input.gameObject, input, f.Name, (int)f.GetValue(input),
                        PropertyTrackerData.PropertyTrackerDataOptions.IsInt);

                f.SetValue(input, value);
            }
            catch (Exception e) { _logger.LogError(e); }
        }
        #endregion internal setters
    }
}
