using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using Studio;

namespace RSkoi_ComponentUtil.Core
{
    internal static class ComponentUtilCache
    {
        // TODO: this is subpar, cache will contain orphans with null key on deleting item / component
        //       not as important as the tracker though, cache will just get less efficient over time

        // keys are objects the GetXYZ call is made on, only valid for current scene
        // cache is used to circumvent repeated GetComponentsInChildren, GetComponent calls
        internal static readonly Dictionary<GameObject, Transform[]> _transformSearchCache = [];
        internal static readonly Dictionary<GameObject, Transform[]> _transformSearchChildrenCache = [];
        internal static readonly Dictionary<GameObject, Component[]> _componentSearchCache = [];
        // cache is used to circumvent repeated GetFields, GetProperties calls (reflection)
        internal static readonly Dictionary<Component, PropertyInfo[]> _propertyInfoSearchCache = [];
        internal static readonly Dictionary<Component, FieldInfo[]> _fieldInfoSearchCache = [];
        internal static readonly Dictionary<object, PropertyInfo[]> _propertyInfoSearchCacheObject = [];
        internal static readonly Dictionary<object, FieldInfo[]> _fieldInfoSearchCacheObject = [];

        // cache is used to avoid expensive reflection, key is Type.FullName
        internal static Dictionary<string, Type> _componentAdderSearchCache = [];

        #region internal
        internal static void ClearCache()
        {
            _transformSearchCache.Clear();
            _transformSearchChildrenCache.Clear();
            _componentSearchCache.Clear();
            //_componentAdderSearchCache.Clear();
            _propertyInfoSearchCache.Clear();
            _fieldInfoSearchCache.Clear();
            _propertyInfoSearchCacheObject.Clear();
            _fieldInfoSearchCacheObject.Clear();
        }

        internal static void ClearComponentFromCache(Component c)
        {
            _propertyInfoSearchCache.Remove(c);
            _fieldInfoSearchCache.Remove(c);

            // TODO: how to remove from property/fieldInfoSearchCacheObject, is it really needed?
        }

        /// <summary>
        /// Gets and caches all addable Types
        /// </summary>
        /// <param name="forceRefresh">Whether to forcefully refresh the cache</param>
        /// <returns>List of addable Types</returns>
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
                    ComponentUtil._logger.LogWarning($"Could not load types of assembly {a.FullName}, skipping");
                }
            }

            return [ .._componentAdderSearchCache.Values ];
        }

        internal static Transform[] GetOrCacheTransforms(ObjectCtrlInfo inputCtrl, bool forceRefresh = false)
        {
            GameObject input = inputCtrl.guideObject.transformTarget.gameObject;

            // this should only return cached transforms of input w/o children transforms
            if (!forceRefresh && _transformSearchCache.TryGetValue(input, out Transform[] value) && value != null)
                return value;

            Transform[] transforms = GetOrCacheTransforms(_transformSearchCache, input, forceRefresh);

            HashSet<Transform> allTnoChildrenTransforms = GetOrCacheChildrenTransformsOfTno(inputCtrl.treeNodeObject);
            transforms = [.. transforms.Where(t => !allTnoChildrenTransforms.Contains(t))];

            // overwrite cache with filtered list w/o children transforms
            _transformSearchCache[input] = transforms;
            return transforms;
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

        internal static PropertyInfo[] GetOrCachePropertyInfosObject(object input, bool forceRefresh = false)
        {
            if (!forceRefresh && _propertyInfoSearchCacheObject.TryGetValue(input, out PropertyInfo[] value) && value != null)
                return value;
            if (forceRefresh)
                _propertyInfoSearchCacheObject.Clear();

            PropertyInfo[] res = input
                .GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            _propertyInfoSearchCacheObject.Add(input, res);
            return res;
        }

        internal static FieldInfo[] GetOrCacheFieldInfosObject(object input, bool forceRefresh = false)
        {
            if (!forceRefresh && _fieldInfoSearchCacheObject.TryGetValue(input, out FieldInfo[] value) && value != null)
                return value;
            if (forceRefresh)
                _fieldInfoSearchCacheObject.Clear();

            FieldInfo[] res = input
                .GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            _fieldInfoSearchCacheObject.Add(input, res);
            return res;
        }
        #endregion internal

        #region private
        private static HashSet<Transform> GetOrCacheChildrenTransformsOfTno(TreeNodeObject tnoRoot)
        {
            List<Transform> allChildTransforms = [];
            tnoRoot.child.ForEach(
                t => allChildTransforms.AddRange(
                    // using different bucket as GetComponentsInChildren will pick up further children
                    GetOrCacheTransforms(_transformSearchChildrenCache, Studio.Studio.Instance.dicInfo[t].guideObject.transformTarget.gameObject)
                )
            );
            // no recursion needed because of GetComponentsInChildren being used in GetOrCacheTransformers
            return [.. allChildTransforms];
        }

        private static Transform[] GetOrCacheTransforms(Dictionary<GameObject, Transform[]> cacheBucket, GameObject input, bool forceRefresh = false)
        {
            if (!forceRefresh && cacheBucket.TryGetValue(input, out Transform[] value) && value != null)
                return value;
            if (forceRefresh)
                cacheBucket.Clear();

            Transform[] res = input.GetComponentsInChildren<Transform>();
            cacheBucket.Add(input, res);
            return res;
        }
        #endregion private

        #region internal helper
        internal static void PrintCache()
        {
            ComponentUtil._logger.LogInfo("+++++++ cache entries");

            foreach (var entry in _transformSearchCache)
                ComponentUtil._logger.LogInfo($"+ transforms: {entry.Key} , length {entry.Value.Length}");

            foreach (var entry in _componentSearchCache)
                ComponentUtil._logger.LogInfo($"+ components: {entry.Key} , length {entry.Value.Length}");

            foreach (Component c in _propertyInfoSearchCache.Keys)
                ComponentUtil._logger.LogInfo($"+ propertyInfos: {c}");

            foreach (Component c in _fieldInfoSearchCache.Keys)
                ComponentUtil._logger.LogInfo($"+ fieldInfos: {c}");

            foreach (object o in _propertyInfoSearchCacheObject.Keys)
                ComponentUtil._logger.LogInfo($"+ referencePropertyInfos: {o}");

            foreach (object o in _fieldInfoSearchCacheObject.Keys)
                ComponentUtil._logger.LogInfo($"+ referenceFieldInfos: {o}");

            //foreach (string c in _componentAdderSearchCache.Keys)
            //    ComponentUtil.logger.LogInfo($"+ componentAdder: {c}");

            ComponentUtil._logger.LogInfo("+++++++");
        }
        #endregion internal helper
    }
}
