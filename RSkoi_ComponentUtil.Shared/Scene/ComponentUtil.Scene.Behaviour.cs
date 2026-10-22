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
using RSkoi_ComponentUtil.Timeline;
using static RSkoi_ComponentUtil.ComponentUtil;
using static RSkoi_ComponentUtil.Scene.ComponentUtilSerializableObjects;

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
                _instance.ResetState();

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

                OnSceneLoadAddComponents(deserializedTrackerDataDict, loadedItems);
                //StartCoroutine(OnSceneLoadAddComponentsRoutine(deserializedTrackerDataDict, loadedItems));
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
                var loadedItem = loadedItems[entry.Key];
                var loadedItemTransformTarget = loadedItem.guideObject.transformTarget;

                foreach (var componentEntry in entry.Value)
                {
                    // TODO: multiple transforms with the same name as siblings? use propEntry.sublingIndex?
                    var loadedItemEditTransform = string.IsNullOrEmpty(componentEntry.parentPath)
                        ? loadedItemTransformTarget
                        : loadedItemTransformTarget.Find(componentEntry.parentPath);
                    if (loadedItemEditTransform == null)
                    {
                        _logger.LogError($"Could not find transform with path {loadedItemTransformTarget.name}/{componentEntry.parentPath}");
                        continue;
                    }

                    foreach (var componentAddEntry in componentEntry.addedComponents)
                    {
                        // _componentAdderSearchCache is populated once on plugin init (ComponentUtil.LoadedEvent)
                        if (!ComponentUtilCache._componentAdderSearchCache.TryGetValue(componentAddEntry.componentName, out var type))
                        {
                            _logger.LogError($"Component {componentAddEntry.componentName} was not present in cache, cannot add it");
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

        IEnumerator OnSceneLoadAddComponentsRoutine(
            SortedDictionary<int, List<TrackerComponentDataSO>> deserializedTrackerDataDict,
            ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
        {
            yield return new WaitForSeconds(WaitTimeLoadSceneValue);

            OnSceneLoadAddComponents(deserializedTrackerDataDict, loadedItems);
        }

        IEnumerator OnSceneLoadEditPropsRoutine(
            SortedDictionary<int, List<TrackerDataSO>> deserializedTrackerDataDict,
            ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
        {
            yield return new WaitForSeconds(WaitTimeLoadSceneValue);

            // this is beyond horrid
            foreach (var entry in deserializedTrackerDataDict)
            {
                var loadedItem = loadedItems[entry.Key];
                var loadedItemTransformTarget = loadedItem.guideObject.transformTarget;

                foreach (var propEntry in entry.Value)
                {
                    // TODO: multiple transforms with the same name as siblings? use propEntry.sublingIndex?
                    var loadedItemEditTransform = string.IsNullOrEmpty(propEntry.parentPath)
                        ? loadedItemTransformTarget
                        : loadedItemTransformTarget.Find(propEntry.parentPath);
                    if (loadedItemEditTransform == null)
                    {
                        _logger.LogError($"Could not find transform with path {loadedItemTransformTarget.name}/{propEntry.parentPath}");
                        continue;
                    }

                    var component = loadedItemEditTransform.GetComponent(propEntry.componentName);
                    if (component == null)
                    {
                        _logger.LogError($"Could not get component with name {propEntry.componentName} on {loadedItemTransformTarget.name}");
                        continue;
                    }
                    var componentType = component.GetType();
                    bool componentTypeIsRedirector = TypeIsSupportedRedirector(componentType);

                    foreach (var propEdit in propEntry.properties)
                    {
                        bool isReference    = HasPropertyFlag(propEdit.propertyFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsReference);
                        bool isProperty     = HasPropertyFlag(propEdit.propertyFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsProperty);
                        bool isInt          = HasPropertyFlag(propEdit.propertyFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsInt);
                        bool isVector       = !isInt && HasPropertyFlag(propEdit.propertyFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsVector);
                        bool isColor        = HasPropertyFlag(propEdit.propertyFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsColor);

                        var p = (isReference || isProperty) ? componentType.GetProperty(propEdit.propertyName) : null;
                        var f = (isReference || !isProperty) ? componentType.GetField(propEdit.propertyName) : null;

                        var value = _instance.GetValueFieldOrProperty(component, p, f);
                        if (value == null)
                        {
                            _logger.LogWarning($"Could not find non-null {(isProperty ? "property" : "field")} on {loadedItemEditTransform.name}" +
                                $".{componentType.Name} with name {propEdit.propertyName}, ignoring");
                            continue;
                        }

                        if (isInt)
                            value = Convert.ToInt32(value);
                        else if (isVector)
                        {
                            var vectorString = VectorConversion.VectorToStringByType(isProperty ? p.PropertyType : f.FieldType, value);
                            if (vectorString.IsNullOrEmpty())
                            {
                                _logger.LogError($"Failed to convert vector {(isProperty ? "property" : "field")} on {loadedItemEditTransform.name}" +
                                    $".{componentType.Name} with name {propEdit.propertyName}, ignoring");
                                continue;
                            }
                            value = vectorString;
                        }
                        else if (isColor)
                            value = ColorConversion.ColorToString((Color)value);

                        var propEditValueString = InvariantConversion.ToInvariantString(propEdit.propertyValue);
                        // a default value was saved -> can be discarded
                        // redirector values are always set
                        if (!componentTypeIsRedirector && InvariantConversion.ToInvariantString(value) == propEditValueString)
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

                        // dummy properties for reference types don't have a valid value to be set
                        if (isReference)
                            continue;

                        SetValueWithFlags(p, f, component, propEditValueString, propEdit.propertyFlags);
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
                var loadedItem = loadedItems[entry.Key];
                var loadedItemTransformTarget = loadedItem.guideObject.transformTarget;

                foreach (var propEntry in entry.Value)
                {
                    // TODO: multiple transforms with the same name as siblings? use propEntry.sublingIndex?
                    var loadedItemEditTransform = string.IsNullOrEmpty(propEntry.parentPath)
                        ? loadedItemTransformTarget
                        : loadedItemTransformTarget.Find(propEntry.parentPath);
                    if (loadedItemEditTransform == null)
                    {
                        _logger.LogError($"Could not find transform with path {loadedItemTransformTarget.name}/{propEntry.parentPath}");
                        continue;
                    }

                    var component = loadedItemEditTransform.GetComponent(propEntry.componentName);
                    if (component == null)
                    {
                        _logger.LogError($"Could not get component with name {propEntry.componentName} on {loadedItemTransformTarget.name}");
                        continue;
                    }
                    var componentType = component.GetType();
                    bool componentTypeIsRedirector = TypeIsSupportedRedirector(componentType);

                    var propReferenceType = componentType.GetProperty(propEntry.referencePropertyName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var fieldReferenceType = componentType.GetField(propEntry.referencePropertyName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var referenceObject = _instance.GetValueFieldOrProperty(component, propReferenceType, fieldReferenceType);
                    if (referenceObject == null)
                    {
                        _logger.LogWarning($"Could not find non-null reference on {loadedItemEditTransform.name}" +
                            $".{componentType.Name} with name {propEntry.referencePropertyName}, ignoring");
                        continue;
                    }
                    var referenceObjectType = referenceObject.GetType();

                    foreach (var propEdit in propEntry.properties)
                    {
                        // reference properties are not allowed in object inspector
                        var isReference = HasPropertyFlag(propEdit.propertyFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsReference);
                        if (isReference)
                            continue;

                        bool isProperty = HasPropertyFlag(propEdit.propertyFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsProperty);
                        bool isInt      = HasPropertyFlag(propEdit.propertyFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsInt);
                        bool isVector   = !isInt && HasPropertyFlag(propEdit.propertyFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsVector);
                        bool isColor    = HasPropertyFlag(propEdit.propertyFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsColor);

                        var p = isProperty ? referenceObjectType.GetProperty(propEdit.propertyName) : null;
                        var f = !isProperty ? referenceObjectType.GetField(propEdit.propertyName) : null;

                        var value = _instance.GetValueFieldOrProperty(referenceObject, p, f);
                        if (value == null)
                        {
                            _logger.LogWarning($"Could not find non-null reference {(isProperty ? "property" : "field")} on {loadedItemEditTransform.name}" +
                                $".{componentType.Name}.{propEntry.referencePropertyName} with name {propEdit.propertyName}, ignoring");
                            continue;
                        }

                        if (isInt)
                            value = Convert.ToInt32(value);
                        else if (isVector)
                        {
                            var vectorString = VectorConversion.VectorToStringByType(isProperty ? p.PropertyType : f.FieldType, value);
                            if (vectorString.IsNullOrEmpty())
                            {
                                _logger.LogError($"Failed to convert vector {(isProperty ? "property" : "field")} on {loadedItemEditTransform.name}" +
                                    $".{componentType.Name}.{propEntry.referencePropertyName} with name {propEdit.propertyName}, ignoring");
                                continue;
                            }
                            value = vectorString;
                        }
                        else if (isColor)
                            value = ColorConversion.ColorToString((Color)value);

                        var propEditValueString = InvariantConversion.ToInvariantString(propEdit.propertyValue);
                        // a default value was saved -> can be discarded
                        // redirector values are always set
                        if (!componentTypeIsRedirector && InvariantConversion.ToInvariantString(value) == propEditValueString)
                            continue;

                        // adding to tracker must not be done by Setter methods such as SetPropertyValue
                        // on loading scene as we need to track loadedItem, not _selectedObject
                        _instance.AddPropertyToTracker(
                            loadedItem,
                            loadedItemEditTransform.gameObject,
                            component,
                            propEntry.referencePropertyName,
                            propEdit.propertyName,
                            value,
                            propEdit.propertyFlags);

                        SetValueWithFlags(p, f, referenceObject, propEditValueString, propEdit.propertyFlags);
                    }
                }
            }
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

            _instance.ClearInvalidEntriesFromTracker();

            var propertySavedDict = new SortedDictionary<int, List<TrackerDataSO>>();
            foreach (var entry in _propertyTracker)
            {
                var key = entry.Key.ObjCtrlInfo.objectInfo.dicKey;
                var properties = new List<TrackerDataPropertySO>();
                foreach (var propEntry in entry.Value)
                {
                    bool isReference    = HasPropertyFlag(propEntry.Value.OptionFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsReference);
                    bool isProperty     = HasPropertyFlag(propEntry.Value.OptionFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsProperty);
                    bool isInt          = HasPropertyFlag(propEntry.Value.OptionFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsInt);
                    bool isVector       = !isInt && HasPropertyFlag(propEntry.Value.OptionFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsVector);
                    bool isColor        = HasPropertyFlag(propEntry.Value.OptionFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsColor);

                    object value = 0;
                    if (!isReference)
                    {
                        if (isProperty)
                            entry.Key.Component.GetPropertyValue(propEntry.Key, out value);
                        else
                            entry.Key.Component.GetFieldValue(propEntry.Key, out value);

                        if (value == null)
                        {
                            _logger.LogWarning($"Tried to save null value for {(isProperty ? "property" : "field")} {entry.Key.Component.name}.{propEntry.Key}, ignoring");
                            continue;
                        }

                        if (isInt)
                            value = Convert.ToInt32(value);
                        else if (isVector)
                            value = VectorConversion.VectorToStringByType(value.GetType(), value);
                        else if (isColor)
                            value = ColorConversion.ColorToString((Color)value);
                    }

                    var prop = new TrackerDataPropertySO(propEntry.Key, value, propEntry.Value.OptionFlags);
                    properties.Add(prop);
                }

                var container = new TrackerDataSO(
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

            var referencePropertySavedDict = new SortedDictionary<int, List<TrackerReferenceDataSO>>();
            foreach (var entry in _referencePropertyTracker)
            {
                var key = entry.Key.ObjCtrlInfo.objectInfo.dicKey;
                var componentType = entry.Key.Component.GetType();
                var propReferenceType = componentType.GetProperty(entry.Key.ReferencePropertyName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var fieldReferenceType = componentType.GetField(entry.Key.ReferencePropertyName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var referenceObject = _instance.GetValueFieldOrProperty(entry.Key.Component, propReferenceType, fieldReferenceType);
                if (referenceObject == null)
                {
                    _logger.LogWarning($"Tried to save null value dummy reference {entry.Key.Component}.{entry.Key.ReferencePropertyName}, ignoring");
                    continue;
                }

                var referenceObjectType = referenceObject.GetType();

                var properties = new List<TrackerDataPropertySO>();
                foreach (var propEntry in entry.Value)
                {
                    // reference properties are not allowed in object inspector
                    var isReference = HasPropertyFlag(propEntry.Value.OptionFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsReference);
                    if (isReference) continue;

                    bool isProperty = HasPropertyFlag(propEntry.Value.OptionFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsProperty);
                    bool isInt      = HasPropertyFlag(propEntry.Value.OptionFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsInt);
                    bool isVector   = !isInt && HasPropertyFlag(propEntry.Value.OptionFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsVector);
                    bool isColor    = HasPropertyFlag(propEntry.Value.OptionFlags, PropertyTrackerData.PropertyTrackerDataOptions.IsColor);

                    var p = isProperty ? referenceObjectType.GetProperty(propEntry.Key) : null;
                    var f = !isProperty ? referenceObjectType.GetField(propEntry.Key) : null;

                    var value = _instance.GetValueFieldOrProperty(referenceObject, p, f);
                    if (value == null)
                    {
                        _logger.LogWarning($"Could not find non-null {(isProperty ? "property" : "field")} on {entry.Key.Go.name}.{componentType.Name}" +
                            $".{entry.Key.ReferencePropertyName} with name {propEntry.Key}, ignoring");
                        continue;
                    }

                    if (isInt)
                        value = Convert.ToInt32(value);
                    else if (isVector)
                        value = VectorConversion.VectorToStringByType(value.GetType(), value);
                    else if (isColor)
                        value = ColorConversion.ColorToString((Color)value);

                    var prop = new TrackerDataPropertySO(propEntry.Key, value, propEntry.Value.OptionFlags);
                    properties.Add(prop);
                }

                var container = new TrackerReferenceDataSO(
                    key,
                    GetGameObjectPathToRoot(entry.Key.Go.transform, entry.Key.ObjCtrlInfo.guideObject.transformTarget),
                    entry.Key.Go.transform.name,
                    entry.Key.Go.transform.GetSiblingIndex(),
                    componentType.Name,
                    entry.Key.ReferencePropertyName,
                    [.. properties]);

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
                _instance.Entry(objectCtrlInfo[0]);

            base.OnObjectsSelected(objectCtrlInfo);
        }

        protected override void OnObjectDeleted(ObjectCtrlInfo objectCtrlInfo)
        {
            _instance.ClearInvalidEntriesFromTracker(objectCtrlInfo);

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
                //      then clone Dictionary<string, PropertyTrackerData>, i.e. property / field data
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

            if (transform == root) return "";

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
        bool TypeIsSupportedRedirector(Type type)
        {
            return redirectorTypes.ContainsValue(type);
        }

        void PrintPropSavedDict(SortedDictionary<int, List<TrackerDataSO>> savedDict)
        {
            var i = 0;
            foreach (var entry in savedDict)
            {
                _logger.LogInfo($"-------------- Entry {i}:");
                _logger.LogInfo($"Key: {entry.Key}");
                foreach (var propEntry in entry.Value)
                {
                    _logger.LogInfo(propEntry.ToString());
                    foreach (var propInner in propEntry.properties)
                        _logger.LogInfo($"     {propInner}");
                }
                _logger.LogInfo("--------------");
                i++;
            }
        }

        void PrintPropSavedDict(SortedDictionary<int, List<TrackerReferenceDataSO>> savedDict)
        {
            var i = 0;
            foreach (var entry in savedDict)
            {
                _logger.LogInfo($"-------------- Entry {i}:");
                _logger.LogInfo($"Key: {entry.Key}");
                foreach (var propEntry in entry.Value)
                {
                    _logger.LogInfo(propEntry.ToString());
                    foreach (var propInner in propEntry.properties)
                        _logger.LogInfo($"     {propInner}");
                }
                _logger.LogInfo("--------------");
                i++;
            }
        }

        void PrintCompSavedDict(SortedDictionary<int, List<TrackerComponentDataSO>> savedDict)
        {
            var i = 0;
            foreach (var entry in savedDict)
            {
                _logger.LogInfo($"-------------- Entry {i}:");
                _logger.LogInfo($"Key: {entry.Key}");
                foreach (var cEntry in entry.Value)
                {
                    _logger.LogInfo(cEntry.ToString());
                    foreach (var cInner in cEntry.addedComponents)
                        _logger.LogInfo($"     {cInner}");
                }
                _logger.LogInfo("--------------");
                i++;
            }
        }
        #endregion private helpers
    }
}
