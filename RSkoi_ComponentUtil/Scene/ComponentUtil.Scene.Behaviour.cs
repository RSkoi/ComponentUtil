using System;
using System.Reflection;
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
using static RSkoi_ComponentUtil.ComponentUtil;
using static RSkoi_ComponentUtil.Scene.ComponentUtilSerializableObjects;

namespace RSkoi_ComponentUtil.Scene
{
    internal class ComponentUtilSceneBehaviour : SceneCustomFunctionController
    {
        #region override
        protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
        {
            if (operation == SceneOperationKind.Clear || operation == SceneOperationKind.Load)
                _instance.ResetState();

            PluginData data = GetExtendedData();
            if (data == null || operation == SceneOperationKind.Clear)
                return;

            if (!LoadSceneData.Value)
                return;

            #region add all components
            if (data.data.TryGetValue($"{name}_addedComponents", out var componentDict) && componentDict != null)
            {
                SortedDictionary<int, List<TrackerComponentDataSO>> deserializedTrackerDataDict
                    = MessagePackSerializer.Deserialize<SortedDictionary<int, List<TrackerComponentDataSO>>>((byte[])componentDict);

                //PrintCompSavedDict(deserializedTrackerDataDict);

                OnSceneLoadAddComponents(deserializedTrackerDataDict, loadedItems);
                //StartCoroutine(OnSceneLoadAddComponentsRoutine(deserializedTrackerDataDict, loadedItems));
            }
            #endregion add all components

            #region set all edited properties
            if (data.data.TryGetValue(name, out var propertyDict) && propertyDict != null)
            {
                SortedDictionary<int, List<TrackerDataSO>> deserializedTrackerDataDict
                    = MessagePackSerializer.Deserialize<SortedDictionary<int, List<TrackerDataSO>>>((byte[])propertyDict);

                //PrintPropSavedDict(deserializedTrackerDataDict);

                StartCoroutine(OnSceneLoadEditPropsRoutine(deserializedTrackerDataDict, loadedItems));
            }
            #endregion set all edited properties

            if (ComponentUtilUI.CanvasIsActive)
                ComponentUtilUI.HideWindow();
        }

        private void OnSceneLoadAddComponents(
            SortedDictionary<int, List<TrackerComponentDataSO>> deserializedTrackerDataDict,
            ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
        {
            foreach (var entry in deserializedTrackerDataDict)
            {
                ObjectCtrlInfo loadedItem = loadedItems[entry.Key];
                Transform loadedItemTransformTarget = loadedItem.guideObject.transformTarget;

                foreach (var componentEntry in entry.Value)
                {
                    // TODO: multiple transforms with the same name as siblings? use propEntry.sublingIndex?
                    Transform loadedItemEditTransform = (componentEntry.parentPath == "")
                        ? loadedItemTransformTarget : loadedItemTransformTarget.Find(componentEntry.parentPath);
                    if (loadedItemEditTransform == null)
                    {
                        logger.LogError($"Could not find transform with path {loadedItemTransformTarget.name}/{componentEntry.parentPath}");
                        continue;
                    }

                    foreach (var componentAddEntry in componentEntry.addedComponents)
                    {
                        /*Type type;
                        try
                        {
                            type = Type.GetType(componentAddEntry.componentName);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"Could not get type of component {componentAddEntry.componentName}; {ex}");
                            continue;
                        }*/

                        // _componentAdderSearchCache is populated once on plugin init (ComponentUtil.LoadedEvent)
                        if (!ComponentUtilCache._componentAdderSearchCache.TryGetValue(componentAddEntry.componentName, out Type type))
                        {
                            logger.LogError($"Component {componentAddEntry.componentName} was not present in cache, cannot add it");
                            continue;
                        }

                        loadedItemEditTransform.gameObject.AddComponent(type);

                        // adding to tracker must not be done by Setter methods such as SetPropertyValue
                        // on loading scene as we need to track loadedItem, not _selectedObject
                        _instance.AddComponentToTracker(
                            loadedItem,
                            loadedItemEditTransform.gameObject,
                            componentAddEntry.componentName);
                    }
                }
            }
        }

        private IEnumerator OnSceneLoadAddComponentsRoutine(
            SortedDictionary<int, List<TrackerComponentDataSO>> deserializedTrackerDataDict,
            ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
        {
            yield return new WaitForSeconds(WaitTimeLoadSceneValue);

            OnSceneLoadAddComponents(deserializedTrackerDataDict, loadedItems);
        }

        private IEnumerator OnSceneLoadEditPropsRoutine(
            SortedDictionary<int, List<TrackerDataSO>> deserializedTrackerDataDict,
            ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
        {
            yield return new WaitForSeconds(WaitTimeLoadSceneValue);

            // this is beyond horrid
            foreach (var entry in deserializedTrackerDataDict)
            {
                ObjectCtrlInfo loadedItem = loadedItems[entry.Key];
                Transform loadedItemTransformTarget = loadedItem.guideObject.transformTarget;

                foreach (var propEntry in entry.Value)
                {
                    // TODO: multiple transforms with the same name as siblings? use propEntry.sublingIndex?
                    Transform loadedItemEditTransform = (propEntry.parentPath == "")
                        ? loadedItemTransformTarget : loadedItemTransformTarget.Find(propEntry.parentPath);
                    if (loadedItemEditTransform == null)
                    {
                        logger.LogError($"Could not find transform with path {loadedItemTransformTarget.name}/{propEntry.parentPath}");
                        continue;
                    }

                    Component component = loadedItemEditTransform.GetComponent(propEntry.componentName);
                    Type componentType = component.GetType();

                    foreach (var propEdit in propEntry.properties)
                    {
                        bool isProperty = HasPropertyFlag(propEdit.propertyFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsProperty);
                        bool isInt = HasPropertyFlag(propEdit.propertyFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsInt);
                        bool isVector = !isInt && HasPropertyFlag(propEdit.propertyFlags, PropertyTrackerData.PropertyTrackerDataOptions.isVector);

                        PropertyInfo p = isProperty ? componentType.GetProperty(propEdit.propertyName) : null;
                        FieldInfo f = !isProperty ? componentType.GetField(propEdit.propertyName) : null;

                        object value = _instance.GetValueFieldOrProperty(component, p, f);
                        if (value == null)
                        {
                            logger.LogWarning($"Could not find property or field on {loadedItemEditTransform.name}" +
                                $".{componentType.Name} with name {propEdit.propertyName}, ignoring");
                            continue;
                        }

                        if (isInt)
                            value = (int)value;
                        else if (isVector)
                        {
                            string vectorString = VectorConversion.VectorToStringByType(isProperty ? p.PropertyType : f.FieldType, value);
                            if (vectorString.IsNullOrEmpty())
                            {
                                logger.LogError($"Failed to convert vector property or field on {loadedItemEditTransform.name}" +
                                    $".{componentType.Name} with name {propEdit.propertyName}, ignoring");
                                continue;
                            }
                            value = vectorString;
                        }
                        
                        // a default value was saved -> can be discarded
                        if (value.ToString() == propEdit.propertyValue)
                            continue;

                        // adding to tracker must not be done by Setter methods such as SetPropertyValue
                        // on loading scene as we need to track loadedItem, not _selectedObject
                        _instance.AddPropertyToTracker(
                            loadedItem,
                            loadedItemEditTransform.gameObject,
                            component,
                            propEdit.propertyName,
                            value,
                            propEdit.propertyFlags);

                        // no
                        if (isProperty)
                        {
                            if (isInt)
                                _instance.SetPropertyValueInt(p, int.Parse(propEdit.propertyValue), component, false);
                            else if (isVector)
                                _instance.SetVectorPropertyValue(p, propEdit.propertyValue, component, false);
                            else
                                _instance.SetPropertyValue(p, propEdit.propertyValue, component, false);
                        }
                        else
                        {
                            if (isInt)
                                _instance.SetFieldValueInt(f, int.Parse(propEdit.propertyValue), component, false);
                            else if (isVector)
                                _instance.SetVectorFieldValue(f, propEdit.propertyValue, component, false);
                            else
                                _instance.SetFieldValue(f, propEdit.propertyValue, component, false);
                        }
                    }
                }
            }
        }

        protected override void OnSceneSave()
        {
            PluginData data = new();

            if (!SaveSceneData.Value)
            {
                data.data.Clear();
                SetExtendedData(data);
                return;
            }
            
            SortedDictionary<int, List<TrackerDataSO>> propertySavedDict = [];
            foreach (var entry in _propertyTracker)
            {
                int key = entry.Key.ObjCtrlInfo.objectInfo.dicKey;
                List<TrackerDataPropertySO> properties = [];
                foreach (var propEntry in entry.Value)
                {
                    bool isProperty = HasPropertyFlag(propEntry.Value.OptionFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsProperty);
                    bool isInt = HasPropertyFlag(propEntry.Value.OptionFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsInt);
                    bool isVector = !isInt && HasPropertyFlag(propEntry.Value.OptionFlags, PropertyTrackerData.PropertyTrackerDataOptions.isVector);

                    object value = null;
                    
                    if (isProperty)
                        entry.Key.Component.GetPropertyValue(propEntry.Key, out value);
                    else
                        entry.Key.Component.GetFieldValue(propEntry.Key, out value);

                    if (isInt)
                        value = (int)value;
                    else if (isVector)
                        value = VectorConversion.VectorToStringByType(value.GetType(), value);

                    TrackerDataPropertySO prop = new(propEntry.Key, value.ToString(), propEntry.Value.OptionFlags);
                    properties.Add(prop);
                }
                
                TrackerDataSO container = new(
                    key,
                    GetGameObjectPathToRoot(entry.Key.Go.transform, entry.Key.ObjCtrlInfo.guideObject.transformTarget),
                    entry.Key.Go.transform.name,
                    entry.Key.Go.transform.GetSiblingIndex(),
                    entry.Key.Component.GetType().Name,
                    [ ..properties ]);

                if (propertySavedDict.ContainsKey(key))
                    propertySavedDict[key].Add(container);
                else
                    propertySavedDict.Add(key, [ container ]);
            }

            SortedDictionary<int, List<TrackerComponentDataSO>> componentSavedDict = [];
            foreach (var entry in _addedComponentsTracker)
            {
                int key = entry.Key.ObjCtrlInfo.objectInfo.dicKey;
                List<TrackerAddedComponentDataSO> addedComponents = [];
                foreach (string componentName in entry.Value)
                {
                    TrackerAddedComponentDataSO c = new(componentName);
                    addedComponents.Add(c);
                }

                TrackerComponentDataSO container = new(
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

            //PrintPropSavedDict(propertySavedDict);
            //PrintCompSavedDict(componentSavedDict);

            SetExtendedData(data);
        }

        protected override void OnObjectsSelected(List<ObjectCtrlInfo> objectCtrlInfo)
        {
            // force singular selection
            if (objectCtrlInfo.Count != 1)
                return;

            if (ComponentUtilUI.CanvasIsActive)
                _instance.Entry(objectCtrlInfo[0]);

            base.OnObjectsSelected(objectCtrlInfo);
        }

        protected override void OnObjectDeleted(ObjectCtrlInfo objectCtrlInfo)
        {
            // copy keys into separate list to avoid System.InvalidOperationException: out of sync
            foreach (var key in new List<PropertyKey>(_propertyTracker.Keys))
                if (key.ObjCtrlInfo == objectCtrlInfo)
                    _propertyTracker.Remove(key);
            foreach (var key in new List<ComponentAdderKey>(_addedComponentsTracker.Keys))
                if (key.ObjCtrlInfo == objectCtrlInfo)
                    _addedComponentsTracker.Remove(key);

            if (ComponentUtilUI.CanvasIsActive && _selectedObject == objectCtrlInfo)
                ComponentUtilUI.HideWindow();

            base.OnObjectDeleted(objectCtrlInfo);
        }
        #endregion override

        #region private helpers
        private bool HasPropertyFlag(PropertyTrackerData.PropertyTrackerDataOptions input, PropertyTrackerData.PropertyTrackerDataOptions flagToCheck)
        {
            if ((input & flagToCheck) == flagToCheck)
                return true;
            return false;
        }

        private string GetGameObjectPathToRoot(Transform transform, Transform root) {
            // notice that transform.Find already accounts for the root
            // -> do not include it in the path

            if (transform == root)
                return "";

            string path = transform.name;
            while (transform.parent != null && transform.parent != root)
            {
                transform = transform.parent;
                path = $"{transform.name}/{path}";
            }
            return path;
        }

        private void PrintPropSavedDict(SortedDictionary<int, List<TrackerDataSO>> savedDict)
        {
            int i = 0;
            foreach (var entry in savedDict)
            {
                logger.LogInfo($"-------------- Entry {i}:");
                logger.LogInfo($"Key: {entry.Key}");
                foreach (var propEntry in entry.Value)
                {
                    logger.LogInfo(propEntry.ToString());
                    foreach (var propInner in propEntry.properties)
                        logger.LogInfo($"     {propInner}");
                }
                logger.LogInfo("--------------");
                i++;
            }
        }

        private void PrintCompSavedDict(SortedDictionary<int, List<TrackerComponentDataSO>> savedDict)
        {
            int i = 0;
            foreach (var entry in savedDict)
            {
                logger.LogInfo($"-------------- Entry {i}:");
                logger.LogInfo($"Key: {entry.Key}");
                foreach (var cEntry in entry.Value)
                {
                    logger.LogInfo(cEntry.ToString());
                    foreach (var cInner in cEntry.addedComponents)
                        logger.LogInfo($"     {cInner}");
                }
                logger.LogInfo("--------------");
                i++;
            }
        }
        #endregion private helpers
    }
}
