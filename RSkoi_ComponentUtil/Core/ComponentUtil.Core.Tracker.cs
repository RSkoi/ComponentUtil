using System;
using System.Collections.Generic;
using UnityEngine;
using Studio;

using static RSkoi_ComponentUtil.ComponentUtil.PropertyTrackerData;

namespace RSkoi_ComponentUtil
{
    public partial class ComponentUtil
    {
        internal static readonly Dictionary<PropertyKey, Dictionary<string, PropertyTrackerData>> _tracker = [];

        #region internal
        internal void ClearTracker()
        {
            _tracker.Clear();
        }

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

            if (_tracker.ContainsKey(key))
                if (_tracker[key].ContainsKey(propertyName))
                    return false; // property already tracked
                else
                    _tracker[key].Add(propertyName, data); // at least one other property is tracked
            else
                _tracker.Add(key, new() { { propertyName, data } }); // new property

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

            _tracker[key].Remove(propertyName);
            if (_tracker[key].Count == 0)
                _tracker.Remove(key);

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
            if (!_tracker.ContainsKey(key))
                return false;
            if (!_tracker[key].ContainsKey(propertyName))
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
            if (!_tracker.ContainsKey(key))
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
            if (_tracker.ContainsKey(key))
                return _tracker[key][propertyName].DefaultValue;
            return null;
        }

        internal object GetTrackedDefaultValue(PropertyKey key, string propertyName, out object defaultValue)
        {
            defaultValue = GetTrackedDefaultValue(key, propertyName);
            return defaultValue;
        }
        #endregion internal

        #region private helpers
        private void PrintTracker()
        {
            int i = 0;
            foreach (var entry in _tracker)
            {
                logger.LogInfo($"++++++++ Entry {i}:");
                logger.LogInfo(entry.Key.ToString());
                foreach (var propEntry in entry.Value)
                    logger.LogInfo($"    {propEntry.Key} {propEntry.Value}");
                logger.LogInfo("++++++++");
                i++;
            }
        }
        #endregion private helpers

        #region internal property classes
        internal class PropertyTrackerData(string propertyName, PropertyTrackerDataOptions optionFlags, object defaultValue)
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
            }
        }

        internal class PropertyKey(ObjectCtrlInfo objCtrlInfo, GameObject go, Component component)
            : IEquatable<PropertyKey>
        {
            // the overarching item / ObjectCtrl (root)
            public ObjectCtrlInfo ObjCtrlInfo = objCtrlInfo;
            // the GameObject the component resides in
            // you can also get this with Component.gameObject
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
        #endregion internal property classes
    }
}
