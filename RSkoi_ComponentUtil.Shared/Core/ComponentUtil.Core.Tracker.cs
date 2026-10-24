using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Studio;

using static RSkoi_ComponentUtil.PropertyTrackerData;

namespace RSkoi_ComponentUtil
{
    internal static class ComponentUtilTracker
    {
        /// <summary>
        /// Keeps track of properties and fields and their default values.
        /// </summary>
        internal static readonly PropertyEditTracker<PropertyKey> _propertyTracker = [];
        /// <summary>
        /// Keeps track of properties and fields inside reference types and their default values.<br/>
        /// Each tracked reference also has a dummy entry (flag IsReference) in _propertyTracker to mark the reference itself as edited.
        /// </summary>
        internal static readonly PropertyEditTracker<PropertyReferenceKey> _referencePropertyTracker = [];
        /// <summary>
        /// Keeps track of which components were added to which objects.
        /// </summary>
        internal static readonly Dictionary<ComponentAdderKey, HashSet<string>> _addedComponentsTracker = [];

        /// <summary>
        /// Compiles a HashSet of all tracked transforms (from the PropertyKeys).
        /// </summary>
        internal static HashSet<Transform> TrackedTransforms
        {
            get
            {
                var res = new HashSet<Transform>();
                foreach (var key in _propertyTracker.Keys)
                    res.Add(key.Go.transform);
                return res;
            }
        }

        /// <summary>
        /// Compiles a HashSet of all tracked components (from the PropertyKeys).
        /// </summary>
        internal static HashSet<Component> TrackedComponents
        {
            get
            {
                var res = new HashSet<Component>();
                foreach (var key in _propertyTracker.Keys)
                    res.Add(key.Component);
                return res;
            }
        }

        /// <summary>
        /// Whether <paramref name="input"/> contains all flags of <paramref name="flagToCheck"/>.
        /// </summary>
        internal static bool HasPropertyFlag(PropertyTrackerDataOptions input, PropertyTrackerDataOptions flagToCheck)
            => (input & flagToCheck) == flagToCheck;

        /// <summary>
        /// Clears the tracker of all data.
        /// </summary>
        internal static void ClearTracker()
        {
            _propertyTracker.Clear();
            _referencePropertyTracker.Clear();
            _addedComponentsTracker.Clear();
        }

        /// <summary>
        /// Deletes entries from the tracker that have been previously freed and are now null.
        /// </summary>
        /// <param name="objCtrlInfoToDelete">pass this obj to delete it from the tracker if present</param>
        internal static void ClearInvalidEntriesFromTracker(ObjectCtrlInfo objCtrlInfoToDelete = null)
        {
            _propertyTracker.RemoveWhere(key => !key.IsValid() || key.ObjCtrlInfo == objCtrlInfoToDelete);
            _referencePropertyTracker.RemoveWhere(key => !key.IsValid() || key.ObjCtrlInfo == objCtrlInfoToDelete);
            // copy keys into separate list to avoid System.InvalidOperationException: out of sync
            foreach (var key in new List<ComponentAdderKey>(_addedComponentsTracker.Keys))
                if (!key.IsValid() || key.ObjCtrlInfo == objCtrlInfoToDelete)
                    _addedComponentsTracker.Remove(key);
        }

        #region added component tracker
        internal static bool AddComponentToTracker(ObjectCtrlInfo objCtrlInfo, GameObject go, string componentName)
        {
            var key = new ComponentAdderKey(objCtrlInfo, go);
            return AddComponentToTracker(key, componentName);
        }

        internal static bool AddComponentToTracker(ComponentAdderKey key, string componentName)
        {
            if (!_addedComponentsTracker.TryGetValue(key, out var componentNames))
            {
                _addedComponentsTracker.Add(key, [componentName]); // new component
                return true;
            }
            // false if already added
            return componentNames.Add(componentName);
        }

        internal static bool RemoveComponentFromTracker(ObjectCtrlInfo objCtrlInfo, GameObject go, Component component)
        {
            var key = new ComponentAdderKey(objCtrlInfo, go);
            return RemoveComponentFromTracker(key, component);
        }

        internal static bool RemoveComponentFromTracker(ComponentAdderKey key, Component component)
        {
            var componentName = component.GetType().FullName;
            if (!_addedComponentsTracker.TryGetValue(key, out var componentNames) || !componentNames.Remove(componentName))
                return false;
            if (componentNames.Count == 0)
                _addedComponentsTracker.Remove(key);

            // untrack all edited properties on component
            _propertyTracker.Remove(new PropertyKey(key.ObjCtrlInfo, key.Go, component));
            // untrack all edited reference properties on component
            _referencePropertyTracker.RemoveWhere(keyRef =>
                keyRef.ObjCtrlInfo == key.ObjCtrlInfo
                && keyRef.Go == key.Go
                && keyRef.Component == component);

            return true;
        }

        internal static bool ComponentIsTracked(
            ObjectCtrlInfo objCtrlInfo, GameObject go, string componentName)
        {
            var key = new ComponentAdderKey(objCtrlInfo, go);
            return ComponentIsTracked(key, componentName);
        }

        internal static bool ComponentIsTracked(ComponentAdderKey key, string componentName)
            => _addedComponentsTracker.TryGetValue(key, out var componentNames) && componentNames.Contains(componentName);
        #endregion added component tracker

        #region property tracker
        internal static bool AddPropertyToTracker(
            ObjectCtrlInfo objCtrlInfo,
            GameObject go,
            Component component,
            string propertyName,
            object defaultValue,
            PropertyTrackerDataOptions optionFlags = PropertyTrackerDataOptions.None)
            => _propertyTracker.Add(new PropertyKey(objCtrlInfo, go, component), propertyName, defaultValue, optionFlags);

        internal static bool AddPropertyToTracker(PropertyKey key, string propertyName, object defaultValue, PropertyTrackerDataOptions optionFlags = PropertyTrackerDataOptions.None)
            => _propertyTracker.Add(key, propertyName, defaultValue, optionFlags);

        internal static bool RemovePropertyFromTracker(ObjectCtrlInfo objCtrlInfo, GameObject go, Component component, string propertyName)
            => _propertyTracker.Remove(new PropertyKey(objCtrlInfo, go, component), propertyName);

        internal static bool RemovePropertyFromTracker(PropertyKey key, string propertyName) => _propertyTracker.Remove(key, propertyName);

        internal static bool PropertyIsTracked(PropertyKey key, string propertyName) => _propertyTracker.IsTracked(key, propertyName);

        /// <returns>encoded default value (see ValueConversion), or null if untracked</returns>
        internal static object GetTrackedDefaultValue(PropertyKey key, string propertyName) => _propertyTracker.GetDefaultValue(key, propertyName);
        #endregion property tracker

        #region reference property tracker
        internal static bool AddPropertyToTracker(
            ObjectCtrlInfo objCtrlInfo,
            GameObject go,
            Component component,
            string referencePropertyName,
            string propertyName,
            object defaultValue,
            PropertyTrackerDataOptions optionFlags = PropertyTrackerDataOptions.None)
        {
            var key = new PropertyReferenceKey(objCtrlInfo, go, component, referencePropertyName);
            return AddPropertyToTracker(key, propertyName, defaultValue, optionFlags);
        }

        internal static bool AddPropertyToTracker(
            PropertyReferenceKey key,
            string propertyName,
            object defaultValue,
            PropertyTrackerDataOptions optionFlags = PropertyTrackerDataOptions.None)
        {
            // add dummy tracker to reference type
            AddPropertyToTracker(key.ObjCtrlInfo, key.Go, key.Component, key.ReferencePropertyName, defaultValue: 0, PropertyTrackerDataOptions.IsReference);
            return _referencePropertyTracker.Add(key, propertyName, defaultValue, optionFlags);
        }

        /// <param name="removedKey">true if this was the last edit inside the reference</param>
        internal static bool RemovePropertyFromTracker(
            PropertyReferenceKey key,
            string propertyName,
            out bool removedKey)
        {
            removedKey = false;
            if (!_referencePropertyTracker.Remove(key, propertyName, out var keyRemoved))
                return false;

            // last edit inside the reference removed -> remove the dummy reference tracker
            if (keyRemoved)
                removedKey = RemovePropertyFromTracker(key.ObjCtrlInfo, key.Go, key.Component, key.ReferencePropertyName);

            return true;
        }

        internal static bool PropertyIsTracked(PropertyReferenceKey key, string propertyName)
            => _referencePropertyTracker.IsTracked(key, propertyName);

        /// <returns>the encoded default value (see ValueConversion), or null if not tracked</returns>
        internal static object GetTrackedDefaultValue(PropertyReferenceKey key, string propertyName)
            => _referencePropertyTracker.GetDefaultValue(key, propertyName);
        #endregion reference property tracker

        /// <summary>
        /// Edited properties / fields and their default values, grouped by key (a component, or a reference inside a component).
        /// </summary>
        internal class PropertyEditTracker<TKey> : IEnumerable<KeyValuePair<TKey, Dictionary<string, PropertyTrackerData>>>
        {
            readonly Dictionary<TKey, Dictionary<string, PropertyTrackerData>> _edits = [];

            public ICollection<TKey> Keys => _edits.Keys;

            /// <summary>
            /// Tracks an edit. If the property is already tracked, its first default value is kept.
            /// </summary>
            /// <param name="defaultValue">encoded value before the first edit (see ValueConversion)</param>
            /// <returns>false if the property was already tracked</returns>
            public bool Add(TKey key, string propertyName, object defaultValue, PropertyTrackerDataOptions flags)
            {
                if (!_edits.TryGetValue(key, out var properties))
                {
                    properties = [];
                    _edits.Add(key, properties);
                }
                else if (properties.ContainsKey(propertyName))
                    return false;

                properties.Add(propertyName, new(propertyName, flags, defaultValue));
                return true;
            }

            /// <returns>false if the property was not tracked</returns>
            public bool Remove(TKey key, string propertyName) => Remove(key, propertyName, out _);

            /// <param name="keyRemoved">true if this was the last tracked property of key, so key was removed too</param>
            /// <returns>false if the property was not tracked</returns>
            public bool Remove(TKey key, string propertyName, out bool keyRemoved)
            {
                keyRemoved = false;
                if (!_edits.TryGetValue(key, out var properties) || !properties.Remove(propertyName))
                    return false;

                if (properties.Count == 0)
                {
                    _edits.Remove(key);
                    keyRemoved = true;
                }
                return true;
            }

            public bool Remove(TKey key) => _edits.Remove(key);

            public void RemoveWhere(Func<TKey, bool> predicate)
            {
                // copy keys into separate list to avoid System.InvalidOperationException: out of sync
                foreach (var key in new List<TKey>(_edits.Keys))
                    if (predicate(key))
                        _edits.Remove(key);
            }

            public bool IsTracked(TKey key, string propertyName)
                => _edits.TryGetValue(key, out var properties) && properties.ContainsKey(propertyName);

            /// <returns>the encoded default value (see ValueConversion), or null if not tracked</returns>
            public object GetDefaultValue(TKey key, string propertyName)
            {
                if (_edits.TryGetValue(key, out var properties) && properties.TryGetValue(propertyName, out var data))
                    return data.DefaultValue;
                return null;
            }

            public bool TryGetValue(TKey key, out Dictionary<string, PropertyTrackerData> properties)
                => _edits.TryGetValue(key, out properties);

            public void Clear() => _edits.Clear();

            public IEnumerator<KeyValuePair<TKey, Dictionary<string, PropertyTrackerData>>> GetEnumerator()
                => _edits.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        #region private helpers
        static void PrintTracker()
        {
            var i = 0;
            Logger.LogInfo($"+++++++++++++++++ Properties:");
            foreach (var entry in _propertyTracker)
            {
                Logger.LogInfo($"++++++++ Entry {i}:");
                Logger.LogInfo(entry.Key.ToString());
                foreach (var propEntry in entry.Value)
                    Logger.LogInfo($"    {propEntry.Key} {propEntry.Value}");
                Logger.LogInfo("++++++++");
                i++;
            }

            Logger.LogInfo($"+++++++++++++++++ Reference Properties:");
            foreach (var entry in _referencePropertyTracker)
            {
                Logger.LogInfo($"++++++++ Entry {i}:");
                Logger.LogInfo(entry.Key.ToString());
                foreach (var propEntry in entry.Value)
                    Logger.LogInfo($"    {propEntry.Key} {propEntry.Value}");
                Logger.LogInfo("++++++++");
                i++;
            }

            Logger.LogInfo($"+++++++++++++++++ Components:");
            foreach (var entry in _addedComponentsTracker)
            {
                Logger.LogInfo($"++++++++ Entry {i}:");
                Logger.LogInfo(entry.Key.ToString());
                foreach (var cEntry in entry.Value)
                    Logger.LogInfo($"    {cEntry}");
                Logger.LogInfo("++++++++");
                i++;
            }
        }
        #endregion private helpers
    }

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
            => $"PropertyTrackerData [ PropertyName: {PropertyName}, Options: {OptionFlags}, DefaultValue: {DefaultValue} ]";

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
                var hash = 17;
                hash = hash * 31 + ObjCtrlInfo.GetHashCode();
                hash = hash * 31 + Go.GetHashCode();
                hash = hash * 31 + Component.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj) => Equals(obj as PropertyKey);

        public bool Equals(PropertyKey other)
        {
            return other != null
               && ObjCtrlInfo   == other.ObjCtrlInfo
               && Go            == other.Go
               && Component     == other.Component;
        }

        public bool IsValid() => ObjCtrlInfo != null && Go != null && Component != null;

        public override string ToString()
            => $"PropertyKey [ ObjCtrlInfo: {ObjCtrlInfo.treeNodeObject.textName}, GameObject: {Go}, Component: {Component} ]";
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
                var hash = 17;
                hash = hash * 31 + ObjCtrlInfo.GetHashCode();
                hash = hash * 31 + Go.GetHashCode();
                hash = hash * 31 + Component.GetHashCode();
                hash = hash * 31 + ReferencePropertyName.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj) => Equals(obj as PropertyReferenceKey);

        public bool Equals(PropertyReferenceKey other)
        {
            return other != null
               && ObjCtrlInfo           == other.ObjCtrlInfo
               && Go                    == other.Go
               && Component             == other.Component
               && ReferencePropertyName == other.ReferencePropertyName;
        }

        public bool IsValid() => ObjCtrlInfo != null && Go != null && Component != null;

        public override string ToString()
            => $"PropertyReferenceKey [ ObjCtrlInfo: {ObjCtrlInfo.treeNodeObject.textName}, GameObject: {Go}, " +
                $"Component: {Component}, ReferencePropertyName: {ReferencePropertyName} ]";
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
                var hash = 17;
                hash = hash * 31 + ObjCtrlInfo.GetHashCode();
                hash = hash * 31 + Go.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj) => Equals(obj as ComponentAdderKey);

        public bool Equals(ComponentAdderKey other)
        {
            return other != null
               && ObjCtrlInfo   == other.ObjCtrlInfo
               && Go            == other.Go;
        }

        public bool IsValid() => ObjCtrlInfo != null && Go != null;

        public override string ToString()
            => $"ComponentKey [ ObjCtrlInfo: {ObjCtrlInfo.treeNodeObject.textName}, GameObject: {Go} ]";
    }
    #endregion component classes
}
