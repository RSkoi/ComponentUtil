using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using KKAPI.Utilities;

using RSkoi_ComponentUtil.UI;
using RSkoi_ComponentUtil.Core;

namespace RSkoi_ComponentUtil
{
    public partial class ComponentUtil
    {
        /// <summary>
        /// Sets up component inspector: configure delete component button, 
        /// get all fields of currently selected component, map to list entries of different types.
        /// </summary>
        /// <param name="input">selected component to list the properties and fields of</param>
        /// <param name="inputUi">selected component ui entry</param>
        internal void GetAllFieldsAndProperties(Component input, ComponentUtilUI.GenericUIListEntry inputUi)
        {
            ComponentUtilUI._componentDeleteButton.interactable = false;
            if (input == null || inputUi == null)
                return;

            #region component delete button
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

            ComponentUtilUI.ResetAndDisablePropertyEntries();

            ComponentUtilUI.UpdateUISelectedText(
                ComponentUtilUI._componentPropertyListSelectedComponentText,
                $"{input.gameObject.name}.{input.GetType().Name}");
            ComponentUtilUI.UpdateUISelectedText(
                ComponentUtilUI._objectPropertyListSelectedText,
                "None");
            _selectedReferencePropertyUiEntry = null;

            // otherwise the color picker from a color prop can produce a null ref
            if (Studio.Studio.Instance.colorPalette.visible)
                Studio.Studio.Instance.colorPalette.visible = false;

            // otherwise changing value in ObjectInspector will fail with exception
            if (ComponentUtilUI._objectInspectorWindow.gameObject.activeSelf)
                ComponentUtilUI._objectInspectorWindow.gameObject.SetActive(false);

            string filter = ComponentUtilUI.PageSearchComponentInspectorInputValue.ToLower();
            foreach (PropertyInfo p in ComponentUtilCache.GetOrCachePropertyInfos(input))
            {
                // indexed properties are annoying
                if (p.GetIndexParameters().Length != 0)
                    continue;

                // ignore properties with private or missing getters
                if (p.GetGetMethod() == null)
                    continue;

                // collections are annoying
                if (typeof(IEnumerable).IsAssignableFrom(p.PropertyType)
                    && !supportedTypes.Contains(p.PropertyType))
                    continue;

                // filter string
                if (filter != "" && !p.Name.ToLower().Contains(filter))
                    continue;

                ConfigurePropertyEntry(_selectedComponentUiEntry, input, p, null);
            }

            foreach (FieldInfo f in ComponentUtilCache.GetOrCacheFieldInfos(input))
            {
                // collections are annoying
                if (typeof(IEnumerable).IsAssignableFrom(f.FieldType)
                    && !supportedTypes.Contains(f.FieldType))
                    continue;

                // filter string
                if (filter != "" && !f.Name.ToLower().Contains(filter))
                    continue;

                ConfigurePropertyEntry(_selectedComponentUiEntry, input, null, f);
            }
        }

        internal object GetValueFieldOrProperty(object input, PropertyInfo p, FieldInfo f)
        {
            bool isProperty = p != null;
            bool isField = f != null;

            try
            {
                if (isProperty)
                    return p.GetValue(input, null);
                if (isField)
                    return f.GetValue(input);
            }
            catch (Exception e) {
                _logger.LogError($"GetValueFieldOrProperty caught exception on reflection: {e}");
                return null;
            }

            _logger.LogError("GetValueFieldOrProperty received neither PropertyInfo nor FieldInfo");
            return null;
        }

        private void ConfigurePropertyEntry(ComponentUtilUI.GenericUIListEntry parentUiEntry, object input, PropertyInfo p, FieldInfo f, bool objectMode = false)
        {
            /* 'objectMode' signifies whether we're populating ObjectInspector (true) or ComponentInspector (false)
            *  'input' is either
            *     - the selected Component, or
            *     - a referenced object to list the properties and fields of
            *  'cInput' is input cast to Component if input is selected Component
            *  'value' is either
            *     - the value of the property / field on the Component, or
            *     - the value of the property / field on the reference type
            */

            bool isProperty = p != null;
            bool isField = f != null;
            if ((!isProperty && !isField) || (isProperty && isField))
            {
                _logger.LogError("ConfigurePropertyEntry: pass either PropertyInfo (X)OR FieldInfo as arguments");
                return;
            }

            if (input == null)
            {
                _logger.LogError("ConfigurePropertyEntry: received input null value for " +
                    $"{(isProperty ? "property" : "field")} {(isProperty ? p.Name : f.Name)}");
                return;
            }

            Component cInput = objectMode ? null : (Component)input;
            if (!objectMode && cInput == null)
            {
                _logger.LogError("ConfigurePropertyEntry: objectMode is false, but could not convert input to Component");
                return;
            }
            
            Type type = isProperty ? p.PropertyType : f.FieldType;
            bool typeIsSupported = supportedTypes.Contains(type);
            bool typeIsSupportedAndRewired = supportedTypesRewireAsReference.Contains(type);
            bool typeIsBlacklisted = blacklistTypes.Contains(type);
            if (typeIsBlacklisted
                || !type.IsEnum && type.IsValueType && !typeIsSupported && !typeIsSupportedAndRewired)
                return;
            
            // properties without set method will be non-interactable except 'generic' reference types
            bool setMethodIsPublic = !isProperty || (p.GetSetMethod() != null);
            string propName = isProperty ? p.Name : f.Name;
            
            object value = GetValueFieldOrProperty(objectMode ? input : cInput, p, f);
            if (value == null)
            {
                SetupNullEntry(isProperty, propName, objectMode);
                return;
            }

            GameObject entryPrefab = ComponentUtilUI.MapPropertyOrFieldToEntryPrefab(type);

            ComponentUtilUI.PropertyUIEntry uiEntry = ComponentUtilUI.GetOrInstantiatePropEntryFromPool(entryPrefab, objectMode);
            GameObject entry = uiEntry.UiGO;
            uiEntry.PropertyName.text = isProperty ? $"[Property] {propName}" : $"[Field] {propName}";

            bool isValidReference = !objectMode                         // don't recurse down from object inspector
                && !typeIsSupported                                     // don't override types with custom property entries
                && (!type.IsValueType || typeIsSupportedAndRewired);    // is reference type or rewired
            PropertyTrackerData.PropertyTrackerDataOptions typeAsFlag = isValidReference
                ? PropertyTrackerData.PropertyTrackerDataOptions.IsReference : MapTypeToTrackerFlag(type);
            if (typeAsFlag == PropertyTrackerData.PropertyTrackerDataOptions.IsReference)
                ConfigReference(parentUiEntry, entry, uiEntry, cInput, p, f, type, setMethodIsPublic, isProperty, value);
            else if (typeAsFlag == PropertyTrackerData.PropertyTrackerDataOptions.IsInt)
                ConfigDropdown(parentUiEntry, entry, uiEntry, objectMode ? input : cInput, p, f, type, setMethodIsPublic, isProperty, value, objectMode);
            else if (typeAsFlag == PropertyTrackerData.PropertyTrackerDataOptions.IsBool)
                ConfigToggle(parentUiEntry, entry, uiEntry, objectMode ? input : cInput, p, f, type, setMethodIsPublic, isProperty, value, objectMode);
            else if (typeAsFlag == PropertyTrackerData.PropertyTrackerDataOptions.IsVector)
            { // do not remove these brackets
                if (!ConfigVector(parentUiEntry, entry, uiEntry, objectMode ? input : cInput, p, f, type, setMethodIsPublic, isProperty, value, objectMode))
                {
                    _logger.LogWarning($"Could not configure vector property entry on {cInput.transform.name}.{cInput.name} with name {propName}," +
                        " destroying entry and returning");
                    Destroy(entry);
                    return;
                }
            }
            else if (typeAsFlag == PropertyTrackerData.PropertyTrackerDataOptions.IsColor)
                ConfigColor(parentUiEntry, entry, uiEntry, objectMode ? input : cInput, p, f, type, setMethodIsPublic, isProperty, value, objectMode);
            // this includes string; default is input field with InputField.ContentType.IntegerNumber
            else if (typeAsFlag == PropertyTrackerData.PropertyTrackerDataOptions.IsInput)
                ConfigInput(parentUiEntry, entry, uiEntry, objectMode ? input : cInput, p, f, type, setMethodIsPublic, isProperty, value, objectMode,
                    MapTypeToInputContentType(type));
            else if (typeAsFlag == PropertyTrackerData.PropertyTrackerDataOptions.None)
            {
                Destroy(entry);
                return;
            }
            
            if (isValidReference) // reference type
            {
                PropertyKey key = new(_selectedObject, cInput.gameObject, cInput);
                // make green if dummy property entry is tracked
                CheckTrackedAndMarkAsEditedReference(key, uiEntry, propName);

                //PropertyReferenceKey keyRef = new(_selectedObject, cInput.gameObject, cInput, propName);
                ConfigResetReference(null, uiEntry, input, p, f, propName, isProperty);
                ConfigTimelineReference(null, uiEntry, input, p, f, propName, isProperty, typeAsFlag);
            }
            else if (objectMode) // property inside reference type / the object inspector
            {
                Component c = (Component)parentUiEntry.UiTarget;
                PropertyReferenceKey key = new(_selectedObject, c.gameObject, c, _selectedReferencePropertyUiEntry.PropertyNameValue);
                // make bg green if value is edited
                CheckTrackedAndMarkAsEdited(key, uiEntry, propName, value);

                ConfigResetReferenceProperty(key, uiEntry, input, p, f, propName, setMethodIsPublic, isProperty);
                ConfigTimelineReferenceProperty(key, uiEntry, input, p, f, propName, setMethodIsPublic, isProperty, typeAsFlag);
            }
            else // property in the component inspector
            {
                PropertyKey key = new(_selectedObject, cInput.gameObject, cInput);
                // make bg green if value is edited
                CheckTrackedAndMarkAsEdited(key, uiEntry, propName, value);

                ConfigReset(key, uiEntry, cInput, p, f, propName, setMethodIsPublic, isProperty);
                ConfigTimeline(key, uiEntry, cInput, p, f, propName, setMethodIsPublic, isProperty, typeAsFlag);
            }

            uiEntry.UiGO.SetActive(true);
        }

        private void SetupNullEntry(bool isProperty, string propName, bool objectMode)
        {
            GameObject nullEntryPrefab = ComponentUtilUI.MapPropertyOrFieldToEntryPrefab(null);
            ComponentUtilUI.PropertyUIEntry uiNullEntry = ComponentUtilUI.GetOrInstantiatePropEntryFromPool(nullEntryPrefab, objectMode);

            uiNullEntry.PropertyName.text = isProperty ? $"[Property] {propName}" : $"[Field] {propName}";
            uiNullEntry.ResetButton.interactable = false;
            uiNullEntry.TimelineButton.interactable = false;

            uiNullEntry.UiGO.SetActive(true);
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

        private bool TypeIsFloatingPoint(Type type)
        {
            if (type.Equals(typeof(float))
                || type.Equals(typeof(double))
                || type.Equals(typeof(decimal)))
                return true;
            return false;
        }

        private InputField.ContentType MapTypeToInputContentType(Type type)
        {
            if (type.Equals(typeof(string)))
                return InputField.ContentType.Standard;
            else if (TypeIsFloatingPoint(type))
                return InputField.ContentType.DecimalNumber;
            else
                return InputField.ContentType.IntegerNumber; // trying to cram an integer into e.g. a short will lead to problems
        }

        private PropertyTrackerData.PropertyTrackerDataOptions MapTypeToTrackerFlag(Type type)
        {
            if (type.IsEnum)
                return PropertyTrackerData.PropertyTrackerDataOptions.IsInt;
            else if (type.Equals(typeof(bool)))
                return PropertyTrackerData.PropertyTrackerDataOptions.IsBool;
            else if (type.Equals(typeof(Vector2)) || type.Equals(typeof(Vector3))
                  || type.Equals(typeof(Vector4)) || type.Equals(typeof(Quaternion)))
                return PropertyTrackerData.PropertyTrackerDataOptions.IsVector;
            else if (type.Equals(typeof(Color)))
                return PropertyTrackerData.PropertyTrackerDataOptions.IsColor;
            else if (type.Equals(typeof(AnimationCurve)))
                // TODO: spline editor for UnityEngine.AnimationCurve type
                return PropertyTrackerData.PropertyTrackerDataOptions.None;
            else // this includes string; default is input field with InputField.ContentType.IntegerNumber
                return PropertyTrackerData.PropertyTrackerDataOptions.IsInput;
        }

        #region internal setters
        internal void SetPropertyValue(PropertyInfo p, object value, object input)
        {
            try
            {
                p.SetValue(input, Convert.ChangeType(value, p.PropertyType), null);
            }
            catch (Exception e)
            {
                _logger.LogError(e);
                _logger.LogError($"Tried to set value {value} on {input}.{p.Name}");
            }
        }

        internal void SetPropertyValueInt(PropertyInfo p, int value, object input)
        {
            try
            {
                p.SetValue(input, value, null);
            }
            catch (Exception e) {
                _logger.LogError(e);
                _logger.LogError($"Tried to set value {value} on {input}.{p.Name}");
            }
        }

        internal void SetFieldValue(FieldInfo f, object value, object input)
        {
            try
            {
                f.SetValue(input, Convert.ChangeType(value, f.FieldType));
            }
            catch (Exception e) {
                _logger.LogError(e);
                _logger.LogError($"Tried to set value {value} on {input}.{f.Name}");
            }
        }

        internal void SetFieldValueInt(FieldInfo f, int value, object input)
        {
            try
            {
                f.SetValue(input, value);
            }
            catch (Exception e) {
                _logger.LogError(e);
                _logger.LogError($"Tried to set value {value} on {input}.{f.Name}");
            }
        }
        #endregion internal setters

        #region internal helpers
        internal static bool HasPropertyFlag(
            PropertyTrackerData.PropertyTrackerDataOptions input,
            PropertyTrackerData.PropertyTrackerDataOptions flagToCheck)
        {
            if ((input & flagToCheck) == flagToCheck)
                return true;
            return false;
        }

        internal static void SetValueWithFlags(
            PropertyInfo p,
            FieldInfo f,
            object targetObject,
            string valueString,
            PropertyTrackerData.PropertyTrackerDataOptions flags)
        {
            if (p == null && f == null)
            {
                _logger.LogError("SetValueWithFlags was passed neither PropertyInfo nor FieldInfo");
                return;
            }
            if (targetObject == null)
            {
                _logger.LogError("SetValueWithFlags was passed a null targetObject");
                return;
            }
            // this shit is beyond cursed
            // this is probably a destroyed Unity object that's not actually been set to null yet
            if (targetObject.ToString() == "null")
                return;

            bool isProperty = HasPropertyFlag(flags, PropertyTrackerData.PropertyTrackerDataOptions.IsProperty);
            bool isInt      = HasPropertyFlag(flags, PropertyTrackerData.PropertyTrackerDataOptions.IsInt);
            bool isVector   = !isInt && HasPropertyFlag(flags, PropertyTrackerData.PropertyTrackerDataOptions.IsVector);
            bool isColor    = HasPropertyFlag(flags, PropertyTrackerData.PropertyTrackerDataOptions.IsColor);

            if (isProperty)
            {
                if (isInt)
                    _instance.SetPropertyValueInt(p, int.Parse(valueString), targetObject);
                else if (isVector)
                    _instance.SetVectorPropertyValue(p, valueString, targetObject);
                else if (isColor)
                    _instance.SetPropertyValue(p, ColorConversion.StringToColor(valueString), targetObject);
                else
                    _instance.SetPropertyValue(p, valueString, targetObject);
            }
            else
            {
                if (isInt)
                    _instance.SetFieldValueInt(f, int.Parse(valueString), targetObject);
                else if (isVector)
                    _instance.SetVectorFieldValue(f, valueString, targetObject);
                else if (isColor)
                    _instance.SetFieldValue(f, ColorConversion.StringToColor(valueString), targetObject);
                else
                    _instance.SetFieldValue(f, valueString, targetObject);
            }
        }
        #endregion internal helpers

        #region pages
        internal void OnFilterComponentInspector()
        {
            if (_selectedComponent == null)
                return;

            GetAllFieldsAndProperties(_selectedComponent, _selectedComponentUiEntry);

            ComponentUtilUI.TraverseAndSetEditedParents();
        }

        internal void OnRefreshComponentInspector()
        {
            if (_selectedComponent == null)
                return;

            ComponentUtilCache.GetOrCachePropertyInfos(_selectedComponent, true);
            ComponentUtilCache.GetOrCacheFieldInfos(_selectedComponent, true);
            GetAllFieldsAndProperties(_selectedComponent, _selectedComponentUiEntry);

            _logger.LogMessage("Force refreshed ComponentInspector cache");

            ComponentUtilUI.TraverseAndSetEditedParents();
        }
        #endregion pages

        #region copy / pasting edits
        internal void OnCopyEdits(bool objectMode = false)
        {
            if (!objectMode)
            {
                PropertyKey key = new(_selectedObject, _selectedGO, _selectedComponent);
                if (!_propertyTracker.TryGetValue(key, out var propEdits))
                {
                    _logger.LogMessage("No property or field edits to copy");
                    return;
                }

                var values = GetPropValuesOfObj(_selectedComponent, propEdits);
                _currentCopiedEdits = new(values);

                ComponentUtilUI._componentPropertyPasteButton.interactable = true;
                ComponentUtilUI._objectPropertyPasteButton.interactable = true;

                _logger.LogMessage($"Copied property and field edits of {_selectedGO.name}.{_selectedComponent.GetType().Name}");
            }
            else
            {
                PropertyReferenceKey refKey = new(_selectedObject, _selectedGO, _selectedComponent, _selectedReferencePropertyUiEntry.PropertyNameValue);
                if (!_referencePropertyTracker.TryGetValue(refKey, out var propEdits))
                {
                    _logger.LogMessage("No property or field edits to copy");
                    return;
                }

                var values = GetPropValuesOfObj(_selectedReferencePropertyUiEntry.Wrapper, propEdits);
                _currentCopiedEdits = new(values);

                ComponentUtilUI._componentPropertyPasteButton.interactable = true;
                ComponentUtilUI._objectPropertyPasteButton.interactable = true;

                _logger.LogMessage($"Copied property and field edits of {_selectedGO.name}.{_selectedComponent.GetType().Name}.{_selectedReferencePropertyUiEntry.PropertyNameValue}");
            }
        }

        private Dictionary<string, CopiedEditValueTuple> GetPropValuesOfObj(object obj, Dictionary<string, PropertyTrackerData> trackerData)
        {
            Dictionary<string, CopiedEditValueTuple> values = [];
            foreach (var pair in trackerData)
            {
                bool isReference = HasPropertyFlag(pair.Value.OptionFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsReference);
                if (isReference)
                    continue;

                bool isProperty = HasPropertyFlag(pair.Value.OptionFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsProperty);
                bool isInt = HasPropertyFlag(pair.Value.OptionFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsInt);
                bool isVector = !isInt && HasPropertyFlag(pair.Value.OptionFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsVector);
                bool isColor = HasPropertyFlag(pair.Value.OptionFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsColor);

                object value;
                if (isProperty)
                    obj.GetPropertyValue(pair.Key, out value);
                else
                    obj.GetFieldValue(pair.Key, out value);

                if (value == null)
                    continue;

                if (isInt)
                    value = (int)value;
                else if (isVector)
                    value = VectorConversion.VectorToStringByType(value.GetType(), value);
                else if (isColor)
                    value = ColorConversion.ColorToString((Color)value);

                values.Add(pair.Key, new(value, pair.Value.OptionFlags));
            }
            return values;
        }

        internal void OnPasteEdits(bool objectMode = false)
        {
            if (_currentCopiedEdits == null)
            {
                ComponentUtilUI._componentPropertyPasteButton.interactable = false;
                ComponentUtilUI._objectPropertyPasteButton.interactable = false;
                return;
            }

            if (objectMode)
                OnPasteEditsObject(new(_selectedObject, _selectedGO, _selectedComponent, _selectedReferencePropertyUiEntry.PropertyNameValue));
            else
                OnPasteEditsComponent(new(_selectedObject, _selectedGO, _selectedComponent));

            OnRefreshComponentInspector();
        }

        private void OnPasteEditsComponent(PropertyKey key)
        {
            Component target = _selectedComponent;

            var propsInfosTarget = ComponentUtilCache.GetOrCachePropertyInfos(target).ToDictionary(p => p.Name, p => p);
            var fieldInfosTarget = ComponentUtilCache.GetOrCacheFieldInfos(target).ToDictionary(f => f.Name, f => f);

            foreach (var pair in _currentCopiedEdits.PropEdits)
            {
                string editedPropName = pair.Key;
                object value = pair.Value.Value;

                if (propsInfosTarget.TryGetValue(editedPropName, out var targetProp))
                {
                    target.GetPropertyValue(editedPropName, out var defaultValue);
                    AddPropertyToTracker(key, editedPropName, defaultValue, pair.Value.OptionFlags);
                    SetValueWithFlags(targetProp, null, target, value.ToString(), pair.Value.OptionFlags);
                }
                else if (fieldInfosTarget.TryGetValue(editedPropName, out var targetField))
                {
                    target.GetFieldValue(editedPropName, out var defaultValue);
                    AddPropertyToTracker(key, editedPropName, defaultValue, pair.Value.OptionFlags);
                    SetValueWithFlags(null, targetField, target, value.ToString(), pair.Value.OptionFlags);
                }
            }
        }

        private void OnPasteEditsObject(PropertyReferenceKey refKey)
        {
            object target = _selectedReferencePropertyUiEntry.Wrapper;

            var propsInfosTarget = ComponentUtilCache.GetOrCachePropertyInfosObject(target).ToDictionary(p => p.Name, p => p);
            var fieldInfosTarget = ComponentUtilCache.GetOrCacheFieldInfosObject(target).ToDictionary(f => f.Name, f => f);

            foreach (var pair in _currentCopiedEdits.PropEdits)
            {
                string editedPropName = pair.Key;
                object value = pair.Value.Value;

                if (propsInfosTarget.TryGetValue(editedPropName, out var targetProp))
                {
                    target.GetPropertyValue(editedPropName, out var defaultValue);
                    AddPropertyToTracker(
                        _selectedObject,
                        _selectedGO,
                        _selectedComponent,
                        _selectedReferencePropertyUiEntry.PropertyNameValue,
                        editedPropName,
                        defaultValue,
                        pair.Value.OptionFlags);
                    SetValueWithFlags(targetProp, null, target, value.ToString(), pair.Value.OptionFlags);
                }
                else if (fieldInfosTarget.TryGetValue(editedPropName, out var targetField))
                {
                    target.GetFieldValue(editedPropName, out var defaultValue);
                    AddPropertyToTracker(
                        _selectedObject,
                        _selectedGO,
                        _selectedComponent,
                        _selectedReferencePropertyUiEntry.PropertyNameValue,
                        editedPropName,
                        defaultValue,
                        pair.Value.OptionFlags);
                    SetValueWithFlags(null, targetField, target, value.ToString(), pair.Value.OptionFlags);
                }
            }
        }

        internal struct CopiedEditValueTuple(object value, PropertyTrackerData.PropertyTrackerDataOptions flags)
        {
            public object Value = value;
            public PropertyTrackerData.PropertyTrackerDataOptions OptionFlags = flags;
        }

        internal class CopiedEdits(Dictionary<string, CopiedEditValueTuple> propEdits)
        {
            public Dictionary<string, CopiedEditValueTuple> PropEdits = propEdits;
        }
        #endregion copy / pasting edits
    }
}
