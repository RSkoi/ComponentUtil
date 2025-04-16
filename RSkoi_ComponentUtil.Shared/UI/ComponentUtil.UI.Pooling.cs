using System.Collections.Generic;
using UnityEngine;

namespace RSkoi_ComponentUtil.UI
{
    internal static partial class ComponentUtilUI
    {
        #region pools
        // generic button / list entries
        internal readonly static List<GenericUIListEntry> TransformListEntries = [];
        internal readonly static List<GenericUIListEntry> ComponentListEntries = [];
        internal readonly static List<GenericUIListEntry> ComponentAdderListEntries = [];
        // properties and fields
        internal readonly static Dictionary<GameObject, List<PropertyUIEntry>> _componentInspectorPropertyBuckets = [];
        internal readonly static Dictionary<GameObject, List<PropertyUIEntry>> _objectInspectorPropertyBuckets = [];
        #endregion pools

        /// <summary>
        /// destroys the pooled ui components and clears pools, use sparingly (only when scene is reset)
        /// </summary>
        public static void ClearAllEntryPools()
        {
            ClearEntryListData(TransformListEntries);
            ClearEntryListData(ComponentListEntries);
            ClearEntryListData(ComponentAdderListEntries);

            ClearEntryBucketPool(_componentInspectorPropertyBuckets);
            ClearEntryBucketPool(_objectInspectorPropertyBuckets);
        }

        #region property pool
        internal static PropertyUIEntry GetOrInstantiatePropEntryFromPool(GameObject entryPrefab, bool objectMode = false)
        {
            Dictionary<GameObject, List<PropertyUIEntry>> pool = objectMode ?
                _objectInspectorPropertyBuckets : _componentInspectorPropertyBuckets;
            Transform container = objectMode ?
                _objectPropertyListContainer : _componentPropertyListContainer;

            if (pool.TryGetValue(entryPrefab, out var bucket))
            {
                foreach (var entry in bucket)
                    if (!entry.UiGO.activeSelf)
                        return entry;
            }
            else
            {
                bucket = [];
                pool.Add(entryPrefab, bucket);
            }

            GameObject entryGO = GameObject.Instantiate(entryPrefab, container);
            entryGO.SetActive(false);
            PropertyUIEntry uiEntry = PreConfigureNewUiEntry(entryGO, entryPrefab);
            bucket.Add(uiEntry);

            return uiEntry;
        }

        internal static void ResetAndDisablePropertyEntries(bool objectMode = false)
        {
            Dictionary<GameObject, List<PropertyUIEntry>> pool = objectMode ?
                _objectInspectorPropertyBuckets : _componentInspectorPropertyBuckets;

            foreach (var bucket in pool.Values)
                foreach (var entry in bucket)
                {
                    entry.UiGO.SetActive(false);

                    // events will be reset on configuring specific entry type

                    entry.ParentUiEntry = null;
                    entry.ResetOverrideDelegate = null;
                    entry.UiComponentSetValueResetDelegate = null;
                    entry.Wrapper = null;

                    entry.ResetBg();
                }
        }
        #endregion property pool

        #region transform pool
        internal static void PrepareTransformPool(int newEntriesCount)
        {
            ResetAndDisableTransformEntries();

            // instantiate new entries if needed
            if (newEntriesCount > TransformListEntries.Count)
                InstantiateGenericListEntries(
                    newEntriesCount - TransformListEntries.Count,
                    TransformListEntries,
                    _genericListEntryPrefab,
                    _transformListContainer);
        }

        private static void ResetAndDisableTransformEntries()
        {
            foreach (var entry in TransformListEntries)
            {
                entry.UiGO.SetActive(false);
                entry.ResetBg();
            }
        }
        #endregion transform pool

        #region component pool
        internal static void PrepareComponentPool(int newEntriesCount)
        {
            ResetAndDisableComponentEntries();

            // instantiate new entries if needed
            if (newEntriesCount > ComponentListEntries.Count)
                InstantiateGenericListEntries(
                    newEntriesCount - ComponentListEntries.Count,
                    ComponentListEntries,
                    _genericListEntryPrefab,
                    _componentListContainer);
        }

        private static void ResetAndDisableComponentEntries()
        {
            foreach (var entry in ComponentListEntries)
            {
                entry.UiGO.SetActive(false);
                entry.ResetBg();
            }
        }
        #endregion component pool

        #region component adder pool
        internal static void PrepareComponentAdderPool(int newEntriesCount)
        {
            ResetAndDisableComponentAdderEntries();

            // instantiate new entries if needed
            if (newEntriesCount > ComponentAdderListEntries.Count)
                InstantiateGenericListEntries(
                    newEntriesCount - ComponentAdderListEntries.Count,
                    ComponentAdderListEntries,
                    _genericListEntryPrefab,
                    _componentAdderListContainer);
        }

        private static void ResetAndDisableComponentAdderEntries()
        {
            foreach (var entry in ComponentAdderListEntries)
            {
                entry.UiGO.SetActive(false);
                entry.ResetBg();
            }
        }
        #endregion component adder pool

        #region generic
        private static void InstantiateGenericListEntries(
            int count,
            List<GenericUIListEntry> pool,
            GameObject prefab,
            Transform contentContainer)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject entryGO = GameObject.Instantiate(prefab, contentContainer);
                entryGO.SetActive(false);
                // order of disabled pool entries shouldn't matter
                //entryGO.transform.SetAsLastSibling();
                GenericUIListEntry uiEntry = PreConfigureNewGenericUIListEntry(entryGO);
                pool.Add(uiEntry);
            }
        }

        internal static void ClearEntryListData(List<GenericUIListEntry> list)
        {
            foreach (var t in list)
                GameObject.Destroy(t.UiGO);
            list.Clear();
        }

        internal static void ClearEntryBucketPool(Dictionary<GameObject, List<PropertyUIEntry>> pool)
        {
            foreach (var bucket in pool.Values)
            {
                foreach (var entry in bucket)
                    GameObject.Destroy(entry.UiGO);
                bucket.Clear();
            }
            pool.Clear();
        }
        #endregion generic
    }
}
