using System;
using UnityEngine;

using static RSkoi_ComponentUtil.ComponentUtil;
using static RSkoi_ComponentUtil.ComponentUtil.PropertyTrackerData;

namespace RSkoi_ComponentUtil.Core
{
    /// <summary>
    /// What entry a property or field gets in the inspector. Decides UI prefab and how the value is tracked.
    /// </summary>
    internal enum EntryKind
    {
        /// <summary>
        /// Floats, strings, etc.
        /// </summary>
        Input,
        /// <summary>
        /// Dropdown, tracked as int
        /// </summary>
        Enum,
        Bool,
        /// <summary>
        /// Vectors and Quaternions, value tracked as encoded string
        /// </summary>
        Vector,
        /// <summary>
        /// Color button with color picker, value tracked as encoded string
        /// </summary>
        Color,
        /// <summary>
        /// Object reference button that opens the ObjectInspector, types rewired as reference, value tracked is a dummy
        /// </summary> 
        Reference,
    }

    internal static class EntryKinds
    {
        internal static EntryKind Of(Type type)
        {
            if (type.IsEnum)
                return EntryKind.Enum;
            if (type == typeof(bool))
                return EntryKind.Bool;
            if (VectorConversion.IsVectorType(type))
                return EntryKind.Vector;
            if (type == typeof(Color))
                return EntryKind.Color;
            if (type == typeof(string))
                return EntryKind.Input;
            if (!type.IsValueType || supportedTypesRewireAsReference.Contains(type))
                return EntryKind.Reference;
            return EntryKind.Input;
        }

        /// <summary>
        /// Tracker flag to be saved with each edit.
        /// </summary>
        internal static PropertyTrackerDataOptions ToTrackerFlag(this EntryKind kind) => kind switch
        {
            EntryKind.Enum      => PropertyTrackerDataOptions.IsInt,
            EntryKind.Bool      => PropertyTrackerDataOptions.IsBool,
            EntryKind.Vector    => PropertyTrackerDataOptions.IsVector,
            EntryKind.Color     => PropertyTrackerDataOptions.IsColor,
            EntryKind.Reference => PropertyTrackerDataOptions.IsReference,
            _                   => PropertyTrackerDataOptions.IsInput,
        };
    }
}
