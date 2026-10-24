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
        internal static readonly Dictionary<Type, PropertyInfo[]> _propertyInfoCache = [];
        internal static readonly Dictionary<Type, FieldInfo[]> _fieldInfoCache = [];

        // cache is used to avoid expensive reflection, key is Type.FullName
        internal static Dictionary<string, Type> _componentAdderSearchCache = [];

        #region internal
        internal static void ClearCache()
        {
            _transformSearchCache.Clear();
            _transformSearchChildrenCache.Clear();
            _componentSearchCache.Clear();
            // _componentAdderSearchCache is not cleared
            _propertyInfoCache.Clear();
            _fieldInfoCache.Clear();
        }

        /// <summary>
        /// Gets and caches all addable Types
        /// </summary>
        /// <param name="forceRefresh">whether to forcefully refresh the cache</param>
        /// <returns>list of addable Types</returns>
        internal static List<Type> GetOrCacheComponentAdders(bool forceRefresh = false)
        {
            if (!forceRefresh && _componentAdderSearchCache.Count != 0)
                return [ .._componentAdderSearchCache.Values ];
            if (forceRefresh)
                _componentAdderSearchCache.Clear();

            var componentType = typeof(Component);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var a in assemblies)
            {
                try
                {
                    var assemblyTypes = a.GetTypes();
                    // TODO: this still lets through stuff like MonoBehaviour
                    // it will probably crash the game when added as a component
                    // whoops!
                    foreach (var t in assemblyTypes.Where(t => componentType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract && !t.IsInterface))
                        if (!_componentAdderSearchCache.ContainsKey(t.FullName))
                            _componentAdderSearchCache.Add(t.FullName, t);
                }
                catch (ReflectionTypeLoadException)
                {
                    Logger.LogWarning($"Could not load types of assembly {a.FullName}, skipping");
                }
            }

            return [ .._componentAdderSearchCache.Values ];
        }

        internal static Transform[] GetOrCacheTransforms(ObjectCtrlInfo inputCtrl, bool forceRefresh = false)
        {
            var input = inputCtrl.guideObject.transformTarget.gameObject;
            return GetOrCache(_transformSearchCache, input, forceRefresh, _ =>
            {
                var transforms = input.GetComponentsInChildren<Transform>();
                // without transforms of child items in the workspace, they are items of their own
                var allTnoChildrenTransforms = GetOrCacheChildrenTransformsOfTno(inputCtrl.treeNodeObject);
                return [.. transforms.Where(t => !allTnoChildrenTransforms.Contains(t))];
            });
        }

        internal static Component[] GetOrCacheComponents(GameObject input, bool forceRefresh = false)
            => GetOrCache(_componentSearchCache, input, forceRefresh, go => go.GetComponents(typeof(Component)));

        internal static PropertyInfo[] GetOrCachePropertyInfos(object input, bool forceRefresh = false)
            => GetOrCache(_propertyInfoCache, input.GetType(), forceRefresh, t => t.GetProperties(Member.INSTANCE_MEMBERS));

        internal static FieldInfo[] GetOrCacheFieldInfos(object input, bool forceRefresh = false)
            => GetOrCache(_fieldInfoCache, input.GetType(), forceRefresh, t => t.GetFields(Member.INSTANCE_MEMBERS));
        #endregion internal

        #region private
        /// <summary>
        /// Returns cached value, or computes and caches it.
        /// </summary>
        /// <param name="forceRefresh">whether to recompute the value of this key</param>
        static TValue GetOrCache<TKey, TValue>(Dictionary<TKey, TValue> cache, TKey key, bool forceRefresh, Func<TKey, TValue> compute)
            where TValue : class
        {
            if (!forceRefresh && cache.TryGetValue(key, out var value) && value != null)
                return value;

            var res = compute(key);
            cache[key] = res;
            return res;
        }

        static HashSet<Transform> GetOrCacheChildrenTransformsOfTno(TreeNodeObject tnoRoot)
        {
            var allChildTransforms = new List<Transform>();
            foreach (var child in tnoRoot.child)
            {
                if (!Studio.Studio.Instance.dicInfo.ContainsKey(child))
                    continue;

                var childRoot = Studio.Studio.Instance.dicInfo[child].guideObject.transformTarget.gameObject;
                allChildTransforms.AddRange(GetOrCache(
                    _transformSearchChildrenCache, childRoot, forceRefresh: false, go => go.GetComponentsInChildren<Transform>())
                );
            }
            // no recursion because of GetComponentsInChildren
            return [.. allChildTransforms];
        }
        #endregion private

        #region internal helper
        internal static void PrintCache()
        {
            Logger.LogInfo("+++++++ cache entries");

            foreach (var entry in _transformSearchCache)
                Logger.LogInfo($"+ transforms: {entry.Key} , length {entry.Value.Length}");

            foreach (var entry in _componentSearchCache)
                Logger.LogInfo($"+ components: {entry.Key} , length {entry.Value.Length}");

            foreach (var t in _propertyInfoCache.Keys)
                Logger.LogInfo($"+ propertyInfos: {t}");

            foreach (var t in _fieldInfoCache.Keys)
                Logger.LogInfo($"+ fieldInfos: {t}");

            Logger.LogInfo("+++++++");
        }
        #endregion internal helper
    }
}
