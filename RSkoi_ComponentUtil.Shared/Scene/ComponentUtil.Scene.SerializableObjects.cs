using System;
using MessagePack;

using static RSkoi_ComponentUtil.ComponentUtil.PropertyTrackerData;

namespace RSkoi_ComponentUtil.Scene
{
    // keep all of these public, otherwise MessagePack throws MethodAccessException because of PropertyTrackerDataOptions
    public static class ComponentUtilSerializableObjects
    {
        [Serializable]
        [MessagePackObject]
        public class TrackerDataPropertySO(
            string propertyName,
            object propertyValue,
            PropertyTrackerDataOptions propertyFlags = PropertyTrackerDataOptions.None)
        {
            [Key("propertyName")]
            public string propertyName = propertyName;
            [Key("propertyValue")]
            public object propertyValue = propertyValue; // this will probably throw exceptions if object is not serializable
            [Key("propertyFlags")]
            public PropertyTrackerDataOptions propertyFlags = propertyFlags;

            public override string ToString()
            {
                return $"TrackerDataPropertySO [ propertyName: {propertyName}, propertyValue: {propertyValue}, propertyFlags: {propertyFlags} ]";
            }
        }

        [Serializable]
        [MessagePackObject]
        public class TrackerDataSO(
            int parentItemKey,
            string parentPath,
            string objectName,
            int siblingIndex,
            string componentName,
            TrackerDataPropertySO[] properties)
        {
            [Key("parentItemKey")]
            public int parentItemKey = parentItemKey;
            [Key("parentPath")]
            public string parentPath = parentPath;
            [Key("objectName")]
            public string objectName = objectName;
            [Key("siblingIndex")]
            public int siblingIndex = siblingIndex;
            [Key("componentName")]
            public string componentName = componentName;
            [Key("properties")]
            public TrackerDataPropertySO[] properties = properties;

            public override string ToString()
            {
                return $"TrackerDataSO [ parentItemKey: {parentItemKey}, parentPath: {parentPath}, objectName: {objectName}, " +
                    $"siblingIndex: {siblingIndex}, componentName: {componentName}, properties.Length: {properties.Length} ]";
            }
        }

        [Serializable]
        [MessagePackObject]
        public class TrackerReferenceDataSO(
            int parentItemKey,
            string parentPath,
            string objectName,
            int siblingIndex,
            string componentName,
            string referencePropertyName,
            TrackerDataPropertySO[] properties)
        {
            [Key("parentItemKey")]
            public int parentItemKey = parentItemKey;
            [Key("parentPath")]
            public string parentPath = parentPath;
            [Key("objectName")]
            public string objectName = objectName;
            [Key("siblingIndex")]
            public int siblingIndex = siblingIndex;
            [Key("componentName")]
            public string componentName = componentName;
            [Key("referencePropertyName")]
            public string referencePropertyName = referencePropertyName;
            [Key("properties")]
            public TrackerDataPropertySO[] properties = properties;

            public override string ToString()
            {
                return $"TrackerReferenceDataSO [ parentItemKey: {parentItemKey}, parentPath: {parentPath}, objectName: {objectName}, " +
                    $"siblingIndex: {siblingIndex}, componentName: {componentName}, referencePropertyName: {referencePropertyName}, " +
                    $"properties.Length: {properties.Length} ]";
            }
        }

        [Serializable]
        [MessagePackObject]
        public class TrackerAddedComponentDataSO(string componentName)
        {
            [Key("componentName")]
            public string componentName = componentName;

            public override string ToString()
            {
                return $"TrackerDataComponentSO [ componentName: {componentName} ]";
            }
        }

        [Serializable]
        [MessagePackObject]
        public class TrackerComponentDataSO(
            int parentItemKey,
            string parentPath,
            string objectName,
            int siblingIndex,
            TrackerAddedComponentDataSO[] addedComponents)
        {
            [Key("parentItemKey")]
            public int parentItemKey = parentItemKey;
            [Key("parentPath")]
            public string parentPath = parentPath;
            [Key("objectName")]
            public string objectName = objectName;
            [Key("siblingIndex")]
            public int siblingIndex = siblingIndex;
            [Key("addedComponents")]
            public TrackerAddedComponentDataSO[] addedComponents = addedComponents;

            public override string ToString()
            {
                return $"TrackerComponentDataSO [ parentItemKey: {parentItemKey}, parentPath: {parentPath}, objectName: {objectName}, " +
                    $"siblingIndex: {siblingIndex}, addedComponents.Length: {addedComponents.Length} ]";
            }
        }
    }
}
