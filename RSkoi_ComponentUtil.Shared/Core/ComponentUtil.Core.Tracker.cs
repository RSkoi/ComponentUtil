using System;
using System.Collections.Generic;
using UnityEngine;
using Studio;

using static RSkoi_ComponentUtil.ComponentUtil.PropertyTrackerData;

namespace RSkoi_ComponentUtil
{
    public partial class ComponentUtil
    {
        // keeps track of properties and fields and their default values
        internal static readonly Dictionary<PropertyKey, Dictionary<string, PropertyTrackerData>> _propertyTracker = [];
        // keeps track of which components were added to which objects
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
        internal void ClearTracker()
        {
            _propertyTracker.Clear();
            _addedComponentsTracker.Clear();
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

            _propertyTracker.Remove(new(key.ObjCtrlInfo, key.Go, component));

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

        internal object GetTrackedDefaultValue(PropertyKey key, string propertyName, out object defaultValue)
        {
            defaultValue = GetTrackedDefaultValue(key, propertyName);
            return defaultValue;
        }
        #endregion property tracker
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
        // keep this one public, see comment in ComponentUtilSerializableObjects class
        public class PropertyTrackerData(string propertyName, PropertyTrackerDataOptions optionFlags, object defaultValue)
        {
            public string PropertyName = propertyName;
            public PropertyTrackerDataOptions OptionFlags = optionFlags;
            // the default/original value of the property
            public object DefaultValue = defaultValue;

            public override string ToString()
            {
                return $"PropertyTrackerData [ PropertyName: {PropertyName}, Options: {OptionFlags}, DefaultValue: {DefaultValue} ]";
            }

            [Flags]
            public enum PropertyTrackerDataOptions // these are flags, use power of two for values
            {
                None = 0,
                // whether tracked item is a property (true) or a field (false)
                IsProperty = 1,
                // whether value of tracked item should be treated as an integer (convenient for enums)
                IsInt = 2,
                // whether value of tracked item is encoded vector, exclusive with IsInt
                isVector = 4,
            }
        }

        internal class PropertyKey(ObjectCtrlInfo objCtrlInfo, GameObject go, Component component)
            : IEquatable<PropertyKey>
        {
            // the overarching item / ObjectCtrl (root)
            public ObjectCtrlInfo ObjCtrlInfo = objCtrlInfo;
            // the GameObject the component resides in
            // you can also get this with Component.gameObject
            // transform is Go.transform
            public GameObject Go = go;
            // the component the property resides in
            public Component Component = component;

            public override int GetHashCode()
            {
                // oh shit oh fuck
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
                return other != null &&
                   ObjCtrlInfo == other.ObjCtrlInfo &&
                   Go == other.Go &&
                   Component == other.Component;
            }

            public override string ToString()
            {
                return $"PropertyKey [ ObjCtrlInfo: {ObjCtrlInfo}, GameObject: {Go}, Component: {Component} ]";
            }
        }
        #endregion property classes

        #region component classes
        internal class ComponentAdderKey(ObjectCtrlInfo objCtrlInfo, GameObject go)
            : IEquatable<ComponentAdderKey>
        {
            // the overarching item / ObjectCtrl (root)
            public ObjectCtrlInfo ObjCtrlInfo = objCtrlInfo;
            // the GameObject the component resides in
            // transform is Go.transform
            public GameObject Go = go;

            public override int GetHashCode()
            {
                // oh shit oh fuck
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
                return other != null &&
                   ObjCtrlInfo == other.ObjCtrlInfo &&
                   Go == other.Go;
            }

            public override string ToString()
            {
                return $"ComponentKey [ ObjCtrlInfo: {ObjCtrlInfo}, GameObject: {Go} ]";
            }
        }
        #endregion component classes
    }
}
