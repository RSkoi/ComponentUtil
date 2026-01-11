using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Studio;

using static RSkoi_ComponentUtil.ComponentUtil.PropertyTrackerData;

namespace RSkoi_ComponentUtil
{
    public partial class ComponentUtil
    {
        /// <summary>
        /// Keeps track of properties and fields and their default values.
        /// </summary>
        internal static readonly Dictionary<PropertyKey, Dictionary<string, PropertyTrackerData>> _propertyTracker = [];
        /// <summary>
        /// Keeps track of properties and fields and their default values inside reference types.
        /// </summary>
        internal static readonly Dictionary<PropertyReferenceKey, Dictionary<string, PropertyTrackerData>> _referencePropertyTracker = [];
        /// <summary>
        /// Keeps track of which components were added to which objects.
        /// </summary>
        internal static readonly Dictionary<ComponentAdderKey, HashSet<string>> _addedComponentsTracker = [];

        /// <summary>
        /// compiles a HashSet of all tracked transforms (from the PropertyKeys)
        /// </summary>
        internal static HashSet<Transform> TrackedTransforms
        {
            get
            {
                HashSet<Transform> res = [];
                foreach (PropertyKey key in _propertyTracker.Keys)
                    res.Add(key.Go.transform);
                return res;
            }
        }

        /// <summary>
        /// compiles a HashSet of all tracked components (from the PropertyKeys)
        /// </summary>
        internal static HashSet<Component> TrackedComponents
        {
            get
            {
                HashSet<Component> res = [];
                foreach (PropertyKey key in _propertyTracker.Keys)
                    res.Add(key.Component);
                return res;
            }
        }

        #region internal
        internal void CopyEditsForSelectedComponent(
            out Dictionary<string, PropertyTrackerData> props,
            out Dictionary<string, PropertyTrackerData> propsInRefs)
        {
            props = [ ];
            propsInRefs = [ ];

            foreach (var pair in _propertyTracker)
            {
                if (pair.Key.ObjCtrlInfo    != _selectedObject
                    || pair.Key.Go          != _selectedGO
                    || pair.Key.Component   != _selectedComponent)
                    continue;

                props = pair.Value.ToDictionary(
                    e => e.Key, e => new PropertyTrackerData(e.Value.PropertyName, e.Value.OptionFlags, e.Value.DefaultValue));
            }

            foreach (var pair in _referencePropertyTracker)
            {
                if (pair.Key.ObjCtrlInfo                != _selectedObject
                    || pair.Key.Go                      != _selectedGO
                    || pair.Key.Component               != _selectedComponent
                    || pair.Key.ReferencePropertyName   != _selectedReferencePropertyUiEntry.PropertyNameValue)
                    continue;

                propsInRefs = pair.Value.ToDictionary(
                    e => e.Key, e => new PropertyTrackerData(e.Value.PropertyName, e.Value.OptionFlags, e.Value.DefaultValue));
            }
        }

        internal void ClearTracker()
        {
            _propertyTracker.Clear();
            _referencePropertyTracker.Clear();
            _addedComponentsTracker.Clear();
        }

        internal void ClearInvalidEntriesFromTracker(ObjectCtrlInfo objCtrlInfoToDelete = null)
        {
            // copy keys into separate list to avoid System.InvalidOperationException: out of sync
            foreach (var key in new List<PropertyKey>(_propertyTracker.Keys))
                if (!key.IsValid() || key.ObjCtrlInfo == objCtrlInfoToDelete)
                    _propertyTracker.Remove(key);
            foreach (var key in new List<PropertyReferenceKey>(_referencePropertyTracker.Keys))
                if (!key.IsValid() || key.ObjCtrlInfo == objCtrlInfoToDelete)
                    _referencePropertyTracker.Remove(key);
            foreach (var key in new List<ComponentAdderKey>(_addedComponentsTracker.Keys))
                if (!key.IsValid() || key.ObjCtrlInfo == objCtrlInfoToDelete)
                    _addedComponentsTracker.Remove(key);
        }

        #region added component tracker
        internal bool AddComponentToTracker(
            ObjectCtrlInfo objCtrlInfo,
            GameObject go,
            string componentName)
        {
            ComponentAdderKey key = new(objCtrlInfo, go);
            return AddComponentToTracker(key, componentName);
        }

        internal bool AddComponentToTracker(ComponentAdderKey key, string componentName)
        {
            if (_addedComponentsTracker.ContainsKey(key))
                if (_addedComponentsTracker[key].Contains(componentName))
                    return false; // component already added
                else
                    _addedComponentsTracker[key].Add(componentName); // at least one other component is tracked
            else
                _addedComponentsTracker.Add(key, [componentName]); // new component

            return true;
        }

        internal bool RemoveComponentFromTracker(
            ObjectCtrlInfo objCtrlInfo,
            GameObject go,
            Component component)
        {
            ComponentAdderKey key = new(objCtrlInfo, go);
            return RemoveComponentFromTracker(key, component);
        }

        internal bool RemoveComponentFromTracker(ComponentAdderKey key, Component component)
        {
            string componentName = component.GetType().FullName;
            if (!ComponentIsTracked(key, componentName))
                return false;

            _addedComponentsTracker[key].Remove(componentName);
            if (_addedComponentsTracker[key].Count == 0)
                _addedComponentsTracker.Remove(key);

            // untrack all edited properties on component
            _propertyTracker.Remove(new(key.ObjCtrlInfo, key.Go, component));
            // untrack all edited reference properties on component
            foreach (var keyRef in new List<PropertyReferenceKey>(_referencePropertyTracker.Keys))
                if (keyRef.ObjCtrlInfo == key.ObjCtrlInfo
                    && keyRef.Go == key.Go
                    && keyRef.Component == component)
                    _referencePropertyTracker.Remove(keyRef);

            return true;
        }

        internal bool ComponentIsTracked(
            ObjectCtrlInfo objCtrlInfo, GameObject go, string componentName)
        {
            ComponentAdderKey key = new(objCtrlInfo, go);
            return ComponentIsTracked(key, componentName);
        }

        internal bool ComponentIsTracked(ComponentAdderKey key, string componentName)
        {
            if (!_addedComponentsTracker.ContainsKey(key))
                return false;
            if (!_addedComponentsTracker[key].Contains(componentName))
                return false;
            return true;
        }
        #endregion added component tracker

        #region property tracker
        internal bool AddPropertyToTracker(
            ObjectCtrlInfo objCtrlInfo,
            GameObject go,
            Component component,
            string propertyName,
            object defaultValue,
            PropertyTrackerDataOptions optionFlags = PropertyTrackerDataOptions.None)
        {
            PropertyKey key = new(objCtrlInfo, go, component);
            return AddPropertyToTracker(key, propertyName, defaultValue, optionFlags);
        }

        internal bool AddPropertyToTracker(
            PropertyKey key,
            string propertyName,
            object defaultValue,
            PropertyTrackerDataOptions optionFlags = PropertyTrackerDataOptions.None)
        {
            PropertyTrackerData data = new(propertyName, optionFlags, defaultValue);

            if (_propertyTracker.ContainsKey(key))
                if (_propertyTracker[key].ContainsKey(propertyName))
                    return false; // property already tracked
                else
                    _propertyTracker[key].Add(propertyName, data); // at least one other property is tracked
            else
                _propertyTracker.Add(key, new() { { propertyName, data } }); // new property

            return true;
        }

        internal bool RemovePropertyFromTracker(
            ObjectCtrlInfo objCtrlInfo,
            GameObject go,
            Component component,
            string propertyName)
        {
            PropertyKey key = new(objCtrlInfo, go, component);
            return RemovePropertyFromTracker(key, propertyName);
        }

        internal bool RemovePropertyFromTracker(PropertyKey key, string propertyName)
        {
            if (!PropertyIsTracked(key, propertyName))
                return false;

            _propertyTracker[key].Remove(propertyName);
            if (_propertyTracker[key].Count == 0)
                _propertyTracker.Remove(key);

            return true;
        }

        internal bool PropertyIsTracked(
            ObjectCtrlInfo objCtrlInfo, GameObject go, Component component, string propertyName)
        {
            PropertyKey key = new(objCtrlInfo, go, component);
            return PropertyIsTracked(key, propertyName);
        }

        internal bool PropertyIsTracked(PropertyKey key, string propertyName)
        {
            if (!_propertyTracker.ContainsKey(key))
                return false;
            if (!_propertyTracker[key].ContainsKey(propertyName))
                return false;
            return true;
        }

        internal bool TransformObjectAndComponentIsTracked(
            ObjectCtrlInfo objCtrlInfo, GameObject go, Component component)
        {
            PropertyKey key = new(objCtrlInfo, go, component);
            return TransformObjectAndComponentIsTracked(key);
        }

        internal bool TransformObjectAndComponentIsTracked(PropertyKey key)
        {
            if (!_propertyTracker.ContainsKey(key))
                return false;
            return true;
        }

        internal object GetTrackedDefaultValue(
            ObjectCtrlInfo objCtrlInfo,
            GameObject go,
            Component component,
            string propertyName)
        {
            PropertyKey key = new(objCtrlInfo, go, component);
            return GetTrackedDefaultValue(key, propertyName);
        }

        internal object GetTrackedDefaultValue(PropertyKey key, string propertyName)
        {
            if (_propertyTracker.ContainsKey(key))
                return _propertyTracker[key][propertyName].DefaultValue;
            // assumes null as magic value, this could cause problems
            return null;
        }

        internal object GetTrackedDefaultValue(
            PropertyKey key,
            string propertyName,
            out object defaultValue)
        {
            defaultValue = GetTrackedDefaultValue(key, propertyName);
            return defaultValue;
        }
        #endregion property tracker

        #region reference property tracker
        internal bool AddPropertyToTracker(
            ObjectCtrlInfo objCtrlInfo,
            GameObject go,
            Component component,
            string referencePropertyName,
            string propertyName,
            object defaultValue,
            PropertyTrackerDataOptions optionFlags = PropertyTrackerDataOptions.None)
        {
            // add dummy tracker to reference type
            AddPropertyToTracker(objCtrlInfo, go, component, referencePropertyName, 0, PropertyTrackerDataOptions.IsReference);
            PropertyReferenceKey key = new(objCtrlInfo, go, component, referencePropertyName);
            return AddPropertyToTracker(key, propertyName, defaultValue, optionFlags);
        }

        internal bool AddPropertyToTracker(
            PropertyReferenceKey key,
            string propertyName,
            object defaultValue,
            PropertyTrackerDataOptions optionFlags = PropertyTrackerDataOptions.None)
        {
            PropertyTrackerData data = new(propertyName, optionFlags, defaultValue);

            if (_referencePropertyTracker.ContainsKey(key))
                if (_referencePropertyTracker[key].ContainsKey(propertyName))
                    return false; // property already tracked
                else
                    _referencePropertyTracker[key].Add(propertyName, data); // at least one other property is tracked
            else
                _referencePropertyTracker.Add(key, new() { { propertyName, data } }); // new property

            return true;
        }

        internal bool RemovePropertyFromTracker(
            ObjectCtrlInfo objCtrlInfo,
            GameObject go,
            Component component,
            string referencePropertyName,
            string propertyName,
            out bool removedKey)
        {
            RemovePropertyFromTracker(objCtrlInfo, go, component, referencePropertyName);
            PropertyReferenceKey key = new(objCtrlInfo, go, component, referencePropertyName);
            bool ret = RemovePropertyFromTracker(key, propertyName, out bool removedK);
            removedKey = removedK;
            return ret;
        }

        internal bool RemovePropertyFromTracker(
            PropertyReferenceKey key,
            string propertyName,
            out bool removedKey)
        {
            removedKey = false;
            if (!PropertyIsTracked(key, propertyName))
                return false;

            _referencePropertyTracker[key].Remove(propertyName);
            if (_referencePropertyTracker[key].Count == 0)
            {
                _referencePropertyTracker.Remove(key);
                removedKey = RemovePropertyFromTracker(key.ObjCtrlInfo, key.Go, key.Component, key.ReferencePropertyName);
            }

            return true;
        }

        internal bool PropertyIsTracked(
            ObjectCtrlInfo objCtrlInfo,
            GameObject go,
            Component component,
            string referencePropertyName,
            string propertyName)
        {
            PropertyReferenceKey key = new(objCtrlInfo, go, component, referencePropertyName);
            return PropertyIsTracked(key, propertyName)
                && PropertyIsTracked(objCtrlInfo, go, component, referencePropertyName);
        }

        internal bool PropertyIsTracked(PropertyReferenceKey key, string propertyName)
        {
            if (!_referencePropertyTracker.ContainsKey(key))
                return false;
            if (!_referencePropertyTracker[key].ContainsKey(propertyName))
                return false;
            return true;
        }

        internal bool TransformObjectAndComponentIsTracked(
            ObjectCtrlInfo objCtrlInfo,
            GameObject go,
            Component component,
            string referencePropertyName)
        {
            PropertyReferenceKey key = new(objCtrlInfo, go, component, referencePropertyName);
            return TransformObjectAndComponentIsTracked(key)
                && TransformObjectAndComponentIsTracked(objCtrlInfo, go, component);
        }

        internal bool TransformObjectAndComponentIsTracked(PropertyReferenceKey key)
        {
            if (!_referencePropertyTracker.ContainsKey(key))
                return false;
            return true;
        }

        internal object GetTrackedDefaultValue(
            ObjectCtrlInfo objCtrlInfo,
            GameObject go,
            Component component,
            string referencePropertyName,
            string propertyName)
        {
            PropertyReferenceKey key = new(objCtrlInfo, go, component, referencePropertyName);
            return GetTrackedDefaultValue(key, propertyName);
        }

        internal object GetTrackedDefaultValue(PropertyReferenceKey key, string propertyName)
        {
            if (_referencePropertyTracker.ContainsKey(key))
                return _referencePropertyTracker[key][propertyName].DefaultValue;
            // assumes null as magic value, this could cause problems
            return null;
        }

        internal object GetTrackedDefaultValue(
            PropertyReferenceKey key,
            string propertyName,
            out object defaultValue)
        {
            defaultValue = GetTrackedDefaultValue(key, propertyName);
            return defaultValue;
        }
        #endregion reference property tracker
        #endregion internal

        #region private helpers
        private void PrintTracker()
        {
            int i = 0;
            _logger.LogInfo($"+++++++++++++++++ Properties:");
            foreach (var entry in _propertyTracker)
            {
                _logger.LogInfo($"++++++++ Entry {i}:");
                _logger.LogInfo(entry.Key.ToString());
                foreach (var propEntry in entry.Value)
                    _logger.LogInfo($"    {propEntry.Key} {propEntry.Value}");
                _logger.LogInfo("++++++++");
                i++;
            }

            _logger.LogInfo($"+++++++++++++++++ Reference Properties:");
            foreach (var entry in _referencePropertyTracker)
            {
                _logger.LogInfo($"++++++++ Entry {i}:");
                _logger.LogInfo(entry.Key.ToString());
                foreach (var propEntry in entry.Value)
                    _logger.LogInfo($"    {propEntry.Key} {propEntry.Value}");
                _logger.LogInfo("++++++++");
                i++;
            }

            _logger.LogInfo($"+++++++++++++++++ Components:");
            foreach (var entry in _addedComponentsTracker)
            {
                _logger.LogInfo($"++++++++ Entry {i}:");
                _logger.LogInfo(entry.Key.ToString());
                foreach (var cEntry in entry.Value)
                    _logger.LogInfo($"    {cEntry}");
                _logger.LogInfo("++++++++");
                i++;
            }
        }
        #endregion private helpers

        #region property classes
        /// <summary>
        /// Keep this one public, see comment in ComponentUtilSerializableObjects class.
        /// </summary>
        public class PropertyTrackerData(
            string propertyName,
            PropertyTrackerDataOptions optionFlags,
            object defaultValue)
        {
            public string PropertyName = propertyName;
            public PropertyTrackerDataOptions OptionFlags = optionFlags;
            /// <summary>
            /// The default/original value of the property.
            /// </summary>
            public object DefaultValue = defaultValue;

            public override string ToString()
            {
                return $"PropertyTrackerData [ PropertyName: {PropertyName}, Options: {OptionFlags}, DefaultValue: {DefaultValue} ]";
            }

            /// <summary>
            /// These are flags, use power of two for values.
            /// </summary>
            [Flags]
            public enum PropertyTrackerDataOptions
            {
                None = 0,
                /// <summary>
                /// Whether tracked item is a property (true) or a field (false).
                /// </summary>
                IsProperty = 1,
                /// <summary>
                /// Whether value of tracked item should be treated as an integer (convenient for enums).
                /// </summary>
                IsInt = 2,
                /// <summary>
                /// Whether value of tracked item is an encoded vector, exclusive with IsInt.
                /// </summary>
                IsVector = 4,
                /// <summary>
                /// Whether value of tracked item is invalid as it's only used to keep track of edited states of reference types.
                /// </summary>
                IsReference = 8,
                /// <summary>
                /// Whether value of tracked item is a UnityEngine.Color.
                /// </summary>
                IsColor = 16,
                /// <summary>
                /// Whether value of tracked item is a boolean.
                /// </summary>
                IsBool = 32,
                /// <summary>
                /// Whether value of tracked item is of generic input type. This could indicate a string or float, for example.
                /// </summary>
                IsInput = 64,
            }
        }

        internal class PropertyKey(
            ObjectCtrlInfo objCtrlInfo,
            GameObject go,
            Component component)
            : IEquatable<PropertyKey>
        {
            /// <summary>
            /// The overarching item / ObjectCtrl (root).
            /// </summary>
            public ObjectCtrlInfo ObjCtrlInfo = objCtrlInfo;
            /// <summary>
            /// The GameObject the component resides in.
            /// You can also get this with Component.gameObject.
            /// Transform is Go.transform.
            /// </summary>
            public GameObject Go = go;
            /// <summary>
            /// The component the property resides in.
            /// </summary>
            public Component Component = component;

            public override int GetHashCode()
            {
                // https://stackoverflow.com/questions/1646807/quick-and-simple-hash-code-combinations
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + ObjCtrlInfo.GetHashCode();
                    hash = hash * 31 + Go.GetHashCode();
                    hash = hash * 31 + Component.GetHashCode();
                    return hash;
                }
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as PropertyKey);
            }

            public bool Equals(PropertyKey other)
            {
                return other != null
                   && ObjCtrlInfo   == other.ObjCtrlInfo
                   && Go            == other.Go
                   && Component     == other.Component;
            }

            public bool IsValid()
            {
                return ObjCtrlInfo != null && Go != null && Component != null;
            }

            public override string ToString()
            {
                return $"PropertyKey [ ObjCtrlInfo: {ObjCtrlInfo}, GameObject: {Go}, Component: {Component} ]";
            }
        }

        internal class PropertyReferenceKey(
            ObjectCtrlInfo objCtrlInfo,
            GameObject go,
            Component component,
            string referencePropertyName)
            : IEquatable<PropertyReferenceKey>
        {
            /// <summary>
            /// The overarching item / ObjectCtrl (root).
            /// </summary>
            public ObjectCtrlInfo ObjCtrlInfo = objCtrlInfo;
            /// <summary>
            /// The GameObject the component resides in.
            /// You can also get this with Component.gameObject.
            /// Transform is Go.transform.
            /// </summary>
            public GameObject Go = go;
            /// <summary>
            /// The component the property resides in.
            /// </summary>
            public Component Component = component;
            /// <summary>
            /// The name of the reference type property.
            /// </summary>
            public string ReferencePropertyName = referencePropertyName;

            public override int GetHashCode()
            {
                // https://stackoverflow.com/questions/1646807/quick-and-simple-hash-code-combinations
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + ObjCtrlInfo.GetHashCode();
                    hash = hash * 31 + Go.GetHashCode();
                    hash = hash * 31 + Component.GetHashCode();
                    hash = hash * 31 + ReferencePropertyName.GetHashCode();
                    return hash;
                }
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as PropertyReferenceKey);
            }

            public bool Equals(PropertyReferenceKey other)
            {
                return other != null
                   && ObjCtrlInfo           == other.ObjCtrlInfo
                   && Go                    == other.Go
                   && Component             == other.Component
                   && ReferencePropertyName == other.ReferencePropertyName;
            }

            public bool IsValid()
            {
                return ObjCtrlInfo != null && Go != null && Component != null;
            }

            public override string ToString()
            {
                return $"PropertyReferenceKey [ ObjCtrlInfo: {ObjCtrlInfo}, GameObject: {Go}, Component: {Component}, " +
                    $"ReferencePropertyName: {ReferencePropertyName} ]";
            }
        }
        #endregion property classes

        #region component classes
        internal class ComponentAdderKey(ObjectCtrlInfo objCtrlInfo, GameObject go)
            : IEquatable<ComponentAdderKey>
        {
            /// <summary>
            /// The overarching item / ObjectCtrl (root).
            /// </summary>
            public ObjectCtrlInfo ObjCtrlInfo = objCtrlInfo;
            /// <summary>
            /// The GameObject the component resides in.
            /// Transform is Go.transform.
            /// </summary>
            public GameObject Go = go;

            public override int GetHashCode()
            {
                // https://stackoverflow.com/questions/1646807/quick-and-simple-hash-code-combinations
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + ObjCtrlInfo.GetHashCode();
                    hash = hash * 31 + Go.GetHashCode();
                    return hash;
                }
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as ComponentAdderKey);
            }

            public bool Equals(ComponentAdderKey other)
            {
                return other != null
                   && ObjCtrlInfo   == other.ObjCtrlInfo
                   && Go            == other.Go;
            }

            public bool IsValid()
            {
                return ObjCtrlInfo != null && Go != null;
            }

            public override string ToString()
            {
                return $"ComponentKey [ ObjCtrlInfo: {ObjCtrlInfo}, GameObject: {Go} ]";
            }
        }
        #endregion component classes
    }
}
