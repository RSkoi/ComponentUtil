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
using static RSkoi_ComponentUtil.ComponentUtil;
using static RSkoi_ComponentUtil.Scene.ComponentUtilSerializableObjects;

namespace RSkoi_ComponentUtil.Scene
{
    internal class ComponentUtilSceneBehaviour : SceneCustomFunctionController
    {
        private static readonly float WAIT_TIME_AFTER_LOADING_SCENE_SECONDS = 1f;

        #region override
        protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
        {
            if (operation == SceneOperationKind.Clear || operation == SceneOperationKind.Load)
            {
                _instance.ClearTracker();
                _instance.Reset();
            }

            PluginData data = GetExtendedData();
            if (data == null || operation == SceneOperationKind.Clear)
                return;

            if (data.data.TryGetValue(name, out var dict) && dict != null)
            {
                SortedDictionary<int, List<TrackerDataSO>> deserializedTrackerDataDict
                    = MessagePackSerializer.Deserialize<SortedDictionary<int, List<TrackerDataSO>>>((byte[])dict);

                //PrintSavedDict(deserializedTrackerDataDict);

                StartCoroutine(OnSceneLoadRoutine(deserializedTrackerDataDict, loadedItems));
            }
        }

        private IEnumerator OnSceneLoadRoutine(
            SortedDictionary<int, List<TrackerDataSO>> deserializedTrackerDataDict,
            ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
        {
            yield return new WaitForSecondsRealtime(WAIT_TIME_AFTER_LOADING_SCENE_SECONDS);

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
                        bool isInt = HasPropertyFlag(propEdit.propertyFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsInt);
                        bool isProperty = HasPropertyFlag(propEdit.propertyFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsProperty);

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
                        // a default value was saved -> can be discarded
                        if (value.ToString() == propEdit.propertyValue)
                            continue;

                        // adding to tracker must not be done by Setter methods such as SetPropertyValue
                        // on loading scene we need to track loadedItem, not _selectedObject
                        _instance.AddPropertyToTracker(
                            loadedItem,
                            loadedItemEditTransform.gameObject,
                            component,
                            propEdit.propertyName,
                            value,
                            propEdit.propertyFlags);

                        // no
                        if (isProperty)
                            if (isInt)
                                _instance.SetPropertyValueInt(p, int.Parse(propEdit.propertyValue), component, false);
                            else
                                _instance.SetPropertyValue(p, propEdit.propertyValue, component, false);
                        else
                            if (isInt)
                            _instance.SetFieldValueInt(f, int.Parse(propEdit.propertyValue), component, false);
                        else
                            _instance.SetFieldValue(f, propEdit.propertyValue, component, false);
                    }
                }
            }

            if (ComponentUtilUI._canvasContainer.activeSelf)
                ComponentUtilUI.HideWindow();
        }

        protected override void OnSceneSave()
        {
            PluginData data = new();

            SortedDictionary<int, List<TrackerDataSO>> savedDict = [];
            foreach (var entry in _tracker)
            {
                int key = entry.Key.ObjCtrlInfo.objectInfo.dicKey;
                List<TrackerDataPropertySO> properties = [];
                foreach (var propEntry in entry.Value)
                {
                    object value = null;
                    if (HasPropertyFlag(propEntry.Value.OptionFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsProperty))
                        entry.Key.Component.GetPropertyValue(propEntry.Key, out value);
                    else
                        entry.Key.Component.GetFieldValue(propEntry.Key, out value);

                    if (HasPropertyFlag(propEntry.Value.OptionFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsInt))
                        value = (int)value;

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

                if (savedDict.ContainsKey(key))
                    savedDict[key].Add(container);
                else
                    savedDict.Add(key, [ container ]);
            }
            data.data.Add(name, MessagePackSerializer.Serialize(savedDict));

            //PrintSavedDict(savedDict);

            SetExtendedData(data);
        }

        protected override void OnObjectsSelected(List<ObjectCtrlInfo> objectCtrlInfo)
        {
            // force singular selection
            if (objectCtrlInfo.Count != 1)
                return;

            if (ComponentUtilUI._canvasContainer.activeSelf)
                _instance.Entry(objectCtrlInfo[0]);

            base.OnObjectsSelected(objectCtrlInfo);
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

        private void PrintSavedDict(SortedDictionary<int, List<TrackerDataSO>> savedDict)
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
        #endregion private helpers
    }
}
