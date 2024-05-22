using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace RSkoi_ComponentUtil.Core
{
    internal static class ComponentUtilCache
    {
        // keys are objects the GetXYZ call is made on, only valid for current scene
        // cache is used to circumvent repeated GetComponentsInChildren, GetComponent calls
        internal static readonly Dictionary<GameObject, Transform[]> _transformSearchCache = [];
        internal static readonly Dictionary<GameObject, Component[]> _componentSearchCache = [];
        // cache is used to circumvent repeated GetFields, GetProperties calls (reflection)
        internal static readonly Dictionary<Component, PropertyInfo[]> _propertyInfoSearchCache = [];
        internal static readonly Dictionary<Component, FieldInfo[]> _fieldInfoSearchCache = [];

        // cache is used to avoid expensive reflection, key is Type.FullName
        internal static Dictionary<string, Type> _componentAdderSearchCache = [];

        #region internal
        internal static void ClearCache()
        {
            _transformSearchCache.Clear();
            _componentSearchCache.Clear();
            //_componentAdderSearchCache.Clear();
            _propertyInfoSearchCache.Clear();
            _fieldInfoSearchCache.Clear();
        }

        internal static void ClearComponentFromCache(Component c)
        {
            _propertyInfoSearchCache.Remove(c);
            _fieldInfoSearchCache.Remove(c);
        }

        internal static List<Type> GetOrCacheComponentAdders(bool forceRefresh = false)
        {
            if (!forceRefresh && _componentAdderSearchCache.Count != 0)
                return [ .._componentAdderSearchCache.Values ];
            if (forceRefresh)
                _componentAdderSearchCache.Clear();

            Type componentType = typeof(Component);
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly a in assemblies)
            {
                try
                {
                    Type[] assemblyTypes = a.GetTypes();
                    // TODO: this still lets through stuff like MonoBehaviour
                    // it will probably crash the game when added as a component
                    // whoops!
                    foreach (Type t in assemblyTypes.Where(t => componentType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract && !t.IsInterface))
                        if (!_componentAdderSearchCache.ContainsKey(t.FullName))
                            _componentAdderSearchCache.Add(t.FullName, t);
                }
                catch (ReflectionTypeLoadException)
                {
                    ComponentUtil.logger.LogWarning($"Could not load types of assembly {a.FullName}, skipping");
                }
            }

            return [ .._componentAdderSearchCache.Values ];
        }

        internal static Transform[] GetOrCacheTransforms(GameObject input, bool forceRefresh = false)
        {
            if (!forceRefresh && _transformSearchCache.TryGetValue(input, out Transform[] value) && value != null)
                return value;
            if (forceRefresh)
                _transformSearchCache.Clear();

            Transform[] res = input.GetComponentsInChildren<Transform>();
            _transformSearchCache.Add(input, res);
            return res;
        }

        internal static Component[] GetOrCacheComponents(GameObject input, bool forceRefresh = false)
        {
            if (!forceRefresh && _componentSearchCache.TryGetValue(input, out Component[] value) && value != null)
                return value;
            if (forceRefresh)
                _componentSearchCache.Clear();

            Component[] res = input.GetComponents(typeof(Component));
            _componentSearchCache.Add(input, res);
            return res;
        }

        internal static PropertyInfo[] GetOrCachePropertyInfos(Component input, bool forceRefresh = false)
        {
            if (!forceRefresh && _propertyInfoSearchCache.TryGetValue(input, out PropertyInfo[] value) && value != null)
                return value;
            if (forceRefresh)
                _propertyInfoSearchCache.Clear();

            PropertyInfo[] res = input
                .GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            _propertyInfoSearchCache.Add(input, res);
            return res;
        }

        internal static FieldInfo[] GetOrCacheFieldInfos(Component input, bool forceRefresh = false)
        {
            if (!forceRefresh && _fieldInfoSearchCache.TryGetValue(input, out FieldInfo[] value) && value != null)
                return value;
            if (forceRefresh)
                _fieldInfoSearchCache.Clear();

            FieldInfo[] res = input
                .GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            _fieldInfoSearchCache.Add(input, res);
            return res;
        }
        #endregion internal

        #region internal helper
        internal static void PrintCache()
        {
            ComponentUtil.logger.LogInfo("+++++++ cache entries");

            foreach (var entry in _transformSearchCache)
                ComponentUtil.logger.LogInfo($"+ transforms: {entry.Key} , length {entry.Value.Length}");

            foreach (var entry in _componentSearchCache)
                ComponentUtil.logger.LogInfo($"+ components: {entry.Key} , length {entry.Value.Length}");

            foreach (Component c in _propertyInfoSearchCache.Keys)
                ComponentUtil.logger.LogInfo($"+ propertyInfos: {c}");

            foreach (Component c in _fieldInfoSearchCache.Keys)
                ComponentUtil.logger.LogInfo($"+ fieldInfos: {c}");

            //foreach (string c in _componentAdderSearchCache.Keys)
            //    ComponentUtil.logger.LogInfo($"+ componentAdder: {c}");

            ComponentUtil.logger.LogInfo("+++++++");
        }
        #endregion internal helper
    }
}
