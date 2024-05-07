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

        #region internal
        internal static void ClearCache()
        {
            _transformSearchCache.Clear();
            _componentSearchCache.Clear();
            _propertyInfoSearchCache.Clear();
            _fieldInfoSearchCache.Clear();
        }
        
        internal static Transform[] GetOrCacheTransforms(GameObject input, bool forceRefresh = false)
        {
            if (!forceRefresh && _transformSearchCache.TryGetValue(input, out Transform[] value) && value != null)
                return value;

            Transform[] res = input.GetComponentsInChildren<Transform>();
            _transformSearchCache.Add(input, res);
            return res;
        }

        internal static Component[] GetOrCacheComponents(GameObject input, bool forceRefresh = false)
        {
            if (!forceRefresh && _componentSearchCache.TryGetValue(input, out Component[] value) && value != null)
                return value;

            Component[] res = input.GetComponents(typeof(Component));
            _componentSearchCache.Add(input, res);
            return res;
        }

        internal static PropertyInfo[] GetOrCachePropertyInfos(Component input, bool forceRefresh = false)
        {
            if (!forceRefresh && _propertyInfoSearchCache.TryGetValue(input, out PropertyInfo[] value) && value != null)
                return value;

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
            foreach (GameObject go in _transformSearchCache.Keys)
                ComponentUtil.logger.LogInfo($"+ transforms: {go}");
            foreach (GameObject go in _componentSearchCache.Keys)
                ComponentUtil.logger.LogInfo($"+ components: {go}");
            foreach (Component c in _propertyInfoSearchCache.Keys)
                ComponentUtil.logger.LogInfo($"+ propertyInfos: {c}");
            foreach (Component c in _fieldInfoSearchCache.Keys)
                ComponentUtil.logger.LogInfo($"+ fieldInfos: {c}");
            ComponentUtil.logger.LogInfo("+++++++");
        }
        #endregion internal helper
    }
}
