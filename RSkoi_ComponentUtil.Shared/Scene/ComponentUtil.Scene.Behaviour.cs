using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MessagePack;
using ExtensibleSaveFormat;
using Studio;
using KKAPI.Utilities;
using KKAPI.Studio.SaveLoad;

using RSkoi_ComponentUtil.UI;
using RSkoi_ComponentUtil.Core;
using RSkoi_ComponentUtil.Timeline;
using static RSkoi_ComponentUtil.ComponentUtil;
using static RSkoi_ComponentUtil.Scene.ComponentUtilSerializableObjects;
using static RSkoi_ComponentUtil.ComponentUtilEditor;
using static RSkoi_ComponentUtil.ComponentUtilTracker;
using static RSkoi_ComponentUtil.ComponentUtilRedirectors;

namespace RSkoi_ComponentUtil.Scene
{
    internal class ComponentUtilSceneBehaviour : SceneCustomFunctionController
    {
        #region override
        #region load
        protected override void OnSceneLoad(
            SceneOperationKind operation,
            ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
        {
            if (operation == SceneOperationKind.Clear || operation == SceneOperationKind.Load)
                ResetState();

            var data = GetExtendedData();
            if (data == null || operation == SceneOperationKind.Clear)
                return;

            if (!LoadSceneData.Value)
                return;

            #region add all components
            if (data.data.TryGetValue($"{name}_addedComponents", out var componentDict) && componentDict != null)
            {
                var deserializedTrackerDataDict
                    = MessagePackSerializer.Deserialize<SortedDictionary<int, List<TrackerComponentDataSO>>>((byte[])componentDict);
                
                //PrintCompSavedDict(deserializedTrackerDataDict);

                // add components with no wait
                OnSceneLoadAddComponents(deserializedTrackerDataDict, loadedItems);
            }
            #endregion add all components

            #region set all edited properties
            if (data.data.TryGetValue(name, out var propertyDict) && propertyDict != null)
            {
                var deserializedTrackerDataDict
                    = MessagePackSerializer.Deserialize<SortedDictionary<int, List<TrackerDataSO>>>((byte[])propertyDict);

                //PrintPropSavedDict(deserializedTrackerDataDict);

                StartCoroutine(OnSceneLoadEditPropsRoutine(deserializedTrackerDataDict, loadedItems));
            }
            #endregion set all edited properties

            #region set all edited reference properties
            if (data.data.TryGetValue($"{name}_referenceProperties", out var referencePropertyDict) && referencePropertyDict != null)
            {
                var deserializedTrackerDataDict
                    = MessagePackSerializer.Deserialize<SortedDictionary<int, List<TrackerReferenceDataSO>>>((byte[])referencePropertyDict);

                //PrintPropSavedDict(deserializedTrackerDataDict);

                StartCoroutine(OnSceneLoadEditReferencePropsRoutine(deserializedTrackerDataDict, loadedItems));
            }
            #endregion set all edited reference properties

            if (ComponentUtilUI.CanvasIsActive)
                ComponentUtilUI.HideWindow();
        }

        void OnSceneLoadAddComponents(
            SortedDictionary<int, List<TrackerComponentDataSO>> deserializedTrackerDataDict,
            ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
        {
            foreach (var entry in deserializedTrackerDataDict)
            {
                if (!TryGetLoadedItem(loadedItems, entry.Key, out var loadedItem))
                    continue;

                foreach (var componentEntry in entry.Value)
                {
                    var loadedItemEditTransform = FindSavedTransform(loadedItem, componentEntry.parentPath);
                    if (loadedItemEditTransform == null)
                        continue;

                    foreach (var componentAddEntry in componentEntry.addedComponents)
                    {
                        // _componentAdderSearchCache is populated once on plugin init (ComponentUtil.LoadedEvent)
                        if (!ComponentUtilCache._componentAdderSearchCache.TryGetValue(componentAddEntry.componentName, out var type))
                        {
                            Logger.LogError($"Component {componentAddEntry.componentName} was not present in cache, cannot add it");
                            continue;
                        }

                        loadedItemEditTransform.gameObject.AddComponent(type);

                        // adding to tracker must not be done by Setter methods such as SetPropertyValue
                        // on loading scene as we need to track loadedItem, not _selectedObject
                        AddComponentToTracker(
                            loadedItem,
                            loadedItemEditTransform.gameObject,
                            componentAddEntry.componentName);
                    }
                }
            }
        }

        IEnumerator OnSceneLoadEditPropsRoutine(
            SortedDictionary<int, List<TrackerDataSO>> deserializedTrackerDataDict,
            ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
        {
            yield return new WaitForSeconds(WaitTimeLoadSceneValue);

            // this is beyond horrid
            foreach (var entry in deserializedTrackerDataDict)
            {
                if (!TryGetLoadedItem(loadedItems, entry.Key, out var loadedItem))
                    continue;

                foreach (var propEntry in entry.Value)
                {
                    var component = FindSavedComponent(loadedItem, propEntry.parentPath, propEntry.componentName, out var loadedItemEditTransform);
                    if (component == null)
                        continue;
                    var componentType = component.GetType();
                    bool componentTypeIsRedirector = TypeIsSupportedRedirector(componentType);

                    foreach (var propEdit in propEntry.properties)
                    {
                        var flags = propEdit.propertyFlags;
                        // dummy reference entries are added with the edits
                        if (HasPropertyFlag(flags, PropertyTrackerData.PropertyTrackerDataOptions.IsReference))
                            continue;

                        var memberId = SavedMemberId(propEdit);
                        var member = Member.Find(componentType, memberId.Name, memberId.IsProperty);
                        var memberDescription = $"{memberId} on {loadedItemEditTransform.name}.{componentType.Name}";

                        if (!TryReadSavedEdit(propEdit, member, component, componentTypeIsRedirector, memberDescription, out var savedValue, out var encodedValue))
                            continue;

                        /* adding to tracker must not be done by ComponentUtilEditor.ApplyEdit()
                           on loading scene as we need to track loadedItem, not _selectedObject */
                        AddPropertyToTracker(
                            loadedItem,
                            loadedItemEditTransform.gameObject,
                            component,
                            member.Id,
                            encodedValue,
                            flags);

                        ValueConversion.SetDecodedValue(member, component, savedValue);
                    }
                }
            }
        }

        IEnumerator OnSceneLoadEditReferencePropsRoutine(
            SortedDictionary<int, List<TrackerReferenceDataSO>> deserializedTrackerDataDict,
            ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
        {
            yield return new WaitForSeconds(WaitTimeLoadSceneValue);

            // this is beyond horrid
            foreach (var entry in deserializedTrackerDataDict)
            {
                if (!TryGetLoadedItem(loadedItems, entry.Key, out var loadedItem))
                    continue;

                foreach (var propEntry in entry.Value)
                {
                    var component = FindSavedComponent(loadedItem, propEntry.parentPath, propEntry.componentName, out var loadedItemEditTransform);
                    if (component == null)
                        continue;
                    var componentType = component.GetType();
                    bool componentTypeIsRedirector = TypeIsSupportedRedirector(componentType);

                    var reference = propEntry.referenceIsProperty is bool referenceIsProperty
                        ? Member.Find(componentType, propEntry.referencePropertyName, referenceIsProperty)
                        : Member.FindAny(componentType, propEntry.referencePropertyName);
                    var referenceObject = reference?.TryGetValue(component);
                    if (referenceObject == null)
                    {
                        Logger.LogWarning($"Could not find non-null reference on {loadedItemEditTransform.name}" +
                            $".{componentType.Name} with name {propEntry.referencePropertyName}, ignoring");
                        continue;
                    }
                    var referenceObjectType = referenceObject.GetType();

                    foreach (var propEdit in propEntry.properties)
                    {
                        var flags = propEdit.propertyFlags;
                        // reference properties are not allowed in object inspector
                        if (HasPropertyFlag(flags, PropertyTrackerData.PropertyTrackerDataOptions.IsReference))
                            continue;

                        var memberId = SavedMemberId(propEdit);
                        var member = Member.Find(referenceObjectType, memberId.Name, memberId.IsProperty);
                        var memberDescription = $"{memberId} on {loadedItemEditTransform.name}.{componentType.Name}.{propEntry.referencePropertyName}";

                        if (!TryReadSavedEdit(propEdit, member, referenceObject, componentTypeIsRedirector, memberDescription, out var savedValue, out var encodedValue))
                            continue;

                        /* adding to tracker must not be done by ComponentUtilEditor.ApplyEdit()
                           on loading scene as we need to track loadedItem, not _selectedObject */
                        AddPropertyToTracker(
                            loadedItem,
                            loadedItemEditTransform.gameObject,
                            component,
                            reference.Id,
                            member.Id,
                            encodedValue,
                            flags);

                        ValueConversion.SetDecodedValue(member, referenceObject, savedValue);
                    }
                }
            }
        }

        static MemberId SavedMemberId(TrackerDataPropertySO propEdit)
            => new(propEdit.propertyName, HasPropertyFlag(propEdit.propertyFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsProperty));

        /// <param name="alwaysApply">apply even if the saved value equals current value (e.g. redirectors)</param>
        /// <param name="savedValue">decoded saved value to be set</param>
        /// <param name="encodedCurrentValue">encoded current value, default value for the tracker</param>
        /// <returns>whether edit should be applied</returns>
        static bool TryReadSavedEdit(
            TrackerDataPropertySO propEdit,
            Member member,
            object target,
            bool alwaysApply,
            string memberDescription,
            out object savedValue,
            out object encodedCurrentValue)
        {
            savedValue = null;
            encodedCurrentValue = null;
            var flags = propEdit.propertyFlags;

            var currentValue = member?.TryGetValue(target);
            if (currentValue == null)
            {
                Logger.LogWarning($"Could not find non-null {memberDescription}, ignoring");
                return false;
            }

            encodedCurrentValue = ValueConversion.Encode(currentValue, flags);
            if (encodedCurrentValue == null)
            {
                Logger.LogError($"Failed to encode current value of {memberDescription}, ignoring");
                return false;
            }

            try
            {
                if (propEdit.propertyValue != null)
                    savedValue = ValueConversion.Decode(propEdit.propertyValue, member.MemberType, flags);
            }
            catch (Exception e)
            {
                Logger.LogError($"Could not read saved value '{propEdit.propertyValue}' of {memberDescription}, ignoring: {e.Message}");
                return false;
            }
            if (savedValue == null)
            {
                Logger.LogWarning($"Saved value of {memberDescription} is null or of an unsupported type, ignoring");
                return false;
            }

            // a default value was saved -> can be discarded
            return alwaysApply || ValueConversion.EncodeToString(savedValue, flags) != InvariantConversion.ToInvariantString(encodedCurrentValue);
        }
        #endregion load

        #region save
        protected override void OnSceneSave()
        {
            var data = new PluginData();

            if (!SaveSceneData.Value)
            {
                data.data.Clear();
                SetExtendedData(data);
                return;
            }

            ClearInvalidEntriesFromTracker();

            var propertySavedDict = new SortedDictionary<int, List<TrackerDataSO>>();
            foreach (var entry in _propertyTracker)
            {
                var key = entry.Key.ObjCtrlInfo.objectInfo.dicKey;
                var componentType = entry.Key.Component.GetType();
                var properties = new List<TrackerDataPropertySO>();
                foreach (var propEntry in entry.Value)
                {
                    var flags = propEntry.Value.OptionFlags;

                    // dummy reference entries don't have a value
                    object value = 0;
                    if (!HasPropertyFlag(flags, PropertyTrackerData.PropertyTrackerDataOptions.IsReference))
                    {
                        var member = Member.Find(componentType, propEntry.Key.Name, propEntry.Key.IsProperty);
                        value = ValueConversion.Encode(member?.TryGetValue(entry.Key.Component), flags);
                        if (value == null)
                        {
                            Logger.LogWarning($"Tried to save null value for {propEntry.Key} on {entry.Key.Component.name}, ignoring");
                            continue;
                        }
                    }

                    properties.Add(new TrackerDataPropertySO(propEntry.Key.Name, value, flags));
                }

                var container = new TrackerDataSO(
                    key,
                    GetGameObjectPathToRoot(entry.Key.Go.transform, entry.Key.ObjCtrlInfo.guideObject.transformTarget),
                    entry.Key.Go.transform.name,
                    entry.Key.Go.transform.GetSiblingIndex(),
                    componentType.Name,
                    [ ..properties ]);

                if (propertySavedDict.ContainsKey(key))
                    propertySavedDict[key].Add(container);
                else
                    propertySavedDict.Add(key, [ container ]);
            }

            var referencePropertySavedDict = new SortedDictionary<int, List<TrackerReferenceDataSO>>();
            foreach (var entry in _referencePropertyTracker)
            {
                var key = entry.Key.ObjCtrlInfo.objectInfo.dicKey;
                var componentType = entry.Key.Component.GetType();
                var reference = entry.Key.Reference;
                var referenceObject = Member.Find(componentType, reference.Name, reference.IsProperty)?.TryGetValue(entry.Key.Component);
                if (referenceObject == null)
                {
                    Logger.LogWarning($"Tried to save null value dummy reference {entry.Key.Component}.{entry.Key.Reference.Name}, ignoring");
                    continue;
                }

                var referenceObjectType = referenceObject.GetType();

                var properties = new List<TrackerDataPropertySO>();
                foreach (var propEntry in entry.Value)
                {
                    var flags = propEntry.Value.OptionFlags;
                    // reference properties are not allowed in object inspector
                    if (HasPropertyFlag(flags, PropertyTrackerData.PropertyTrackerDataOptions.IsReference))
                        continue;

                    var member = Member.Find(referenceObjectType, propEntry.Key.Name, propEntry.Key.IsProperty);
                    var value = ValueConversion.Encode(member?.TryGetValue(referenceObject), flags);
                    if (value == null)
                    {
                        Logger.LogWarning($"Could not find non-null {propEntry.Key} on {entry.Key.Go.name}.{componentType.Name}.{reference.Name}, ignoring");
                        continue;
                    }

                    properties.Add(new TrackerDataPropertySO(propEntry.Key.Name, value, flags));
                }

                var container = new TrackerReferenceDataSO(
                    key,
                    GetGameObjectPathToRoot(entry.Key.Go.transform, entry.Key.ObjCtrlInfo.guideObject.transformTarget),
                    entry.Key.Go.transform.name,
                    entry.Key.Go.transform.GetSiblingIndex(),
                    componentType.Name,
                    reference.Name,
                    [.. properties])
                {
                    referenceIsProperty = reference.IsProperty,
                };

                if (referencePropertySavedDict.ContainsKey(key))
                    referencePropertySavedDict[key].Add(container);
                else
                    referencePropertySavedDict.Add(key, [container]);
            }

            var componentSavedDict = new SortedDictionary<int, List<TrackerComponentDataSO>>();
            foreach (var entry in _addedComponentsTracker)
            {
                var key = entry.Key.ObjCtrlInfo.objectInfo.dicKey;
                var addedComponents = new List<TrackerAddedComponentDataSO>();
                foreach (var componentName in entry.Value)
                {
                    var c = new TrackerAddedComponentDataSO(componentName);
                    addedComponents.Add(c);
                }

                var container = new TrackerComponentDataSO(
                    key,
                    GetGameObjectPathToRoot(entry.Key.Go.transform, entry.Key.ObjCtrlInfo.guideObject.transformTarget),
                    entry.Key.Go.transform.name,
                    entry.Key.Go.transform.GetSiblingIndex(),
                    [ ..addedComponents ]);

                if (componentSavedDict.ContainsKey(key))
                    componentSavedDict[key].Add(container);
                else
                    componentSavedDict.Add(key, [container]);
            }

            data.data.Add(name, MessagePackSerializer.Serialize(propertySavedDict));
            data.data.Add($"{name}_addedComponents", MessagePackSerializer.Serialize(componentSavedDict));
            data.data.Add($"{name}_referenceProperties", MessagePackSerializer.Serialize(referencePropertySavedDict));

            //PrintPropSavedDict(propertySavedDict);
            //PrintPropSavedDict(referencePropertySavedDict);
            //PrintCompSavedDict(componentSavedDict);

            SetExtendedData(data);
        }
        #endregion save

        protected override void OnObjectsSelected(List<ObjectCtrlInfo> objectCtrlInfo)
        {
            if (!ComponentUtilUI.IsValidSelection(objectCtrlInfo))
                return;

            if (ComponentUtilUI.CanvasIsActive)
                Entry(objectCtrlInfo[0]);

            base.OnObjectsSelected(objectCtrlInfo);
        }

        protected override void OnObjectDeleted(ObjectCtrlInfo objectCtrlInfo)
        {
            ClearInvalidEntriesFromTracker(objectCtrlInfo);

            if (ComponentUtilUI.CanvasIsActive && _selectedObject == objectCtrlInfo)
            {
                ComponentUtilUI.HideWindow();
                _selectedObject = null;
                _selectedGO = null;
                _selectedComponent = null;
            }

            ComponentUtilTimeline.DeleteOciFromCache(objectCtrlInfo);

            base.OnObjectDeleted(objectCtrlInfo);
        }

        protected override void OnObjectsCopied(ReadOnlyDictionary<int, ObjectCtrlInfo> copiedItems)
        {
            // TODO: this is a nightmare

            /*ObjectCtrlInfo selected = null;
            foreach (var entry in copiedItems)
            {
                // if original object (get with id) is tracked by components
                //      for new ComponentAdderKey we need: ObjectCtrlInfo, GameObject
                //      we have to mirror the path to the GameObject on the new hierarchy
                //      then clone HashSet<string>, i.e. names of added components
                //      then add to added components tracker
                //      THEN actually add the components to the GameObject

                // if original object (get with id) is tracked by properties
                //      for new PropertyKey we need: ObjectCtrlInfo, GameObject, Component
                //      mirror new path to GameObject and Component
                //      then clone Dictionary<MemberId, PropertyTrackerData>, i.e. property / field data
                //      remember that the PropertyTrackerData objects probably need to be cloned too
                //      then add to property tracker
                //      THEN apply property / field changes
            }

            if (ComponentUtilUI.CanvasIsActive)
                _instance.Entry(selected);*/

            base.OnObjectsCopied(copiedItems);
        }
        #endregion override

        #region internal helpers
        internal static string GetGameObjectPathToRoot(Transform transform, Transform root)
        {
            // notice that transform.Find already accounts for the root
            // -> do not include it in the path

            if (transform == root)
                return "";

            var path = transform.name;
            while (transform.parent != null && transform.parent != root)
            {
                transform = transform.parent;
                path = $"{transform.name}/{path}";
            }
            return path;
        }
        #endregion internal helpers

        #region private helpers
        static bool TryGetLoadedItem(ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems, int itemKey, out ObjectCtrlInfo loadedItem)
        {
            if (loadedItems.TryGetValue(itemKey, out loadedItem) && loadedItem != null)
                return true;

            Logger.LogError($"Could not find loaded item with key {itemKey}, skipping");
            return false;
        }

        static Transform FindSavedTransform(ObjectCtrlInfo loadedItem, string parentPath)
        {
            var root = loadedItem.guideObject.transformTarget;
            // TODO: multiple transforms with the same name as siblings? use the saved siblingIndex?
            var transform = parentPath == "" ? root : root.Find(parentPath);
            if (transform == null)
                Logger.LogError($"Could not find transform with path {root.name}/{parentPath}");
            return transform;
        }

        static Component FindSavedComponent(ObjectCtrlInfo loadedItem, string parentPath, string componentName, out Transform transform)
        {
            transform = FindSavedTransform(loadedItem, parentPath);
            if (transform == null)
                return null;

            var component = transform.GetComponent(componentName);
            if (component == null)
                Logger.LogError($"Could not get component with name {componentName} on {loadedItem.guideObject.transformTarget.name}");
            return component;
        }

        bool TypeIsSupportedRedirector(Type type) => redirectorTypes.ContainsValue(type);

        void PrintPropSavedDict(SortedDictionary<int, List<TrackerDataSO>> savedDict)
        {
            var i = 0;
            foreach (var entry in savedDict)
            {
                Logger.LogInfo($"-------------- Entry {i}:");
                Logger.LogInfo($"Key: {entry.Key}");
                foreach (var propEntry in entry.Value)
                {
                    Logger.LogInfo(propEntry.ToString());
                    foreach (var propInner in propEntry.properties)
                        Logger.LogInfo($"     {propInner}");
                }
                Logger.LogInfo("--------------");
                i++;
            }
        }

        void PrintPropSavedDict(SortedDictionary<int, List<TrackerReferenceDataSO>> savedDict)
        {
            var i = 0;
            foreach (var entry in savedDict)
            {
                Logger.LogInfo($"-------------- Entry {i}:");
                Logger.LogInfo($"Key: {entry.Key}");
                foreach (var propEntry in entry.Value)
                {
                    Logger.LogInfo(propEntry.ToString());
                    foreach (var propInner in propEntry.properties)
                        Logger.LogInfo($"     {propInner}");
                }
                Logger.LogInfo("--------------");
                i++;
            }
        }

        void PrintCompSavedDict(SortedDictionary<int, List<TrackerComponentDataSO>> savedDict)
        {
            var i = 0;
            foreach (var entry in savedDict)
            {
                Logger.LogInfo($"-------------- Entry {i}:");
                Logger.LogInfo($"Key: {entry.Key}");
                foreach (var cEntry in entry.Value)
                {
                    Logger.LogInfo(cEntry.ToString());
                    foreach (var cInner in cEntry.addedComponents)
                        Logger.LogInfo($"     {cInner}");
                }
                Logger.LogInfo("--------------");
                i++;
            }
        }
        #endregion private helpers
    }
}
