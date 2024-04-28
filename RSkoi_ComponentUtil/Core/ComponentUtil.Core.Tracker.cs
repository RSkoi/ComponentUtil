using System;
using System.Collections.Generic;
using UnityEngine;
using Studio;

using static RSkoi_ComponentUtil.ComponentUtil.PropertyTrackerData;

namespace RSkoi_ComponentUtil
{
    public partial class ComponentUtil
    {
        private static readonly Dictionary<PropertyKey, Dictionary<string, PropertyTrackerData>> _tracker = [];
        internal static Dictionary<PropertyKey, Dictionary<string, PropertyTrackerData>> Tracker { get => _tracker; }

        internal bool AddToTracker(
            ObjectCtrlInfo objCtrlInfo,
            GameObject go,
            Component component,
            string propertyName,
            object defaultValue,
            Options optionFlags = Options.None)
        {
            PropertyKey key = new(objCtrlInfo, go, component);

            PropertyTrackerData data = new(propertyName, optionFlags)
            {
                DefaultValue = defaultValue,
            };

            if (_tracker.ContainsKey(key))
                if (_tracker[key].ContainsKey(propertyName))
                    return false; // property already tracked
                else
                    _tracker[key].Add(propertyName, data); // at least one other property is tracked
            else
                _tracker.Add(key, new() {{ propertyName, data }}); // new property

            return true;
        }

        internal void ClearTracker()
        {
            _tracker.Clear();
        }

        internal object GetTrackedDefaultValue(ObjectCtrlInfo objCtrlInfo, GameObject go, Component component, string propertyName)
        {
            PropertyKey key = new(objCtrlInfo, go, component);

            if (_tracker.ContainsKey(key))
                return _tracker[key][propertyName].DefaultValue;
            return null;
        }

        #region public helpers
        public void PrintTracker()
        {
            int i = 0;
            foreach (var entry in Tracker)
            {
                logger.LogInfo($"++++++++ Entry {i}:");
                logger.LogInfo(entry.Key.Component.gameObject);
                logger.LogInfo(entry.Key.ToString());
                foreach (var propEntry in entry.Value)
                {
                    logger.LogInfo(propEntry.Value.ToString());
                }
                logger.LogInfo("++++++++");
                i++;
            }
        }
        #endregion public helpers

        #region property classes
        internal class PropertyTrackerData(string propertyName, Options optionFlags) : IEquatable<PropertyTrackerData>
        {
            public string PropertyName { get; } = propertyName;
            public Options OptionFlags { get; } = optionFlags;
            // the default/original value of the property
            public object DefaultValue { get; set; }

            public override int GetHashCode()
            {
                return PropertyName.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as PropertyTrackerData);
            }

            public bool Equals(PropertyTrackerData other)
            {
                return other != null && PropertyName == other.PropertyName;
            }

            public override string ToString()
            {
                return $"PropertyTrackerData [ PropertyName: {PropertyName}, Options: {OptionFlags}, DefaultValue: {DefaultValue} ]";
            }

            [Flags]
            internal enum Options // these are flags, use power of two for values
            {
                None = 0,
                // whether tracked item is a property (true) or a field (false)
                IsProperty = 1,
                // whether value of tracked item should be treated as an integer (convenient for enums)
                IsInt = 2,
            }
        }

        internal class PropertyKey(ObjectCtrlInfo objCtrlInfo, GameObject go, Component component) : IEquatable<PropertyKey>
        {
            // the overarching item / ObjectCtrl (root)
            public ObjectCtrlInfo ObjCtrlInfo { get; } = objCtrlInfo;
            // the GameObject the component resides in
            // you can also get this with Component.gameObject
            public GameObject Go { get; } = go;
            // the component the property resides in
            public Component Component { get; } = component;

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
    }
}
