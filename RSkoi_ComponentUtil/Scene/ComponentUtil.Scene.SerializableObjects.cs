using System;
using MessagePack;

namespace RSkoi_ComponentUtil.Scene
{
    internal static class ComponentUtilSerializableObjects
    {
        [Serializable]
        [MessagePackObject]
        internal class TrackerDataPropertySO(
            string propertyName,
            string propertyValue,
            ComponentUtil.PropertyTrackerData.PropertyTrackerDataOptions propertyFlags = ComponentUtil.PropertyTrackerData.PropertyTrackerDataOptions.None)
        {
            [Key("propertyName")]
            public string propertyName = propertyName;
            [Key("propertyValue")]
            public string propertyValue = propertyValue;
            [Key("propertyFlags")]
            public ComponentUtil.PropertyTrackerData.PropertyTrackerDataOptions propertyFlags = propertyFlags;

            public override string ToString()
            {
                return $"TrackerDataPropertySO [ propertyName: {propertyName}, propertyValue: {propertyValue}, propertyFlags: {propertyFlags} ]";
            }
        }

        [Serializable]
        [MessagePackObject]
        internal class TrackerDataSO(
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
    }
}
