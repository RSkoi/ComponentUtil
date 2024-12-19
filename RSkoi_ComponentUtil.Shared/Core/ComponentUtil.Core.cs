using System;
using System.Collections.Generic;
using UnityEngine;

using RSkoi_ComponentUtil.UI;
using RSkoi_ComponentUtil.Core;

namespace RSkoi_ComponentUtil
{
    /// <summary>
    /// class is split into multiple files:<br />
    /// - generic stuff (this file)<br />
    /// - modules (logic related to windows)<br />
    /// - tracker (tracks changes)
    /// </summary>
    public partial class ComponentUtil
    {
        #region currently selected
        internal static Studio.ObjectCtrlInfo _selectedObject;  // selected in workspace
        private static GameObject _selectedGO;                  // selected in TransformList
        private static Component _selectedComponent;            // selected in ComponentList
        
        private static ComponentUtilUI.GenericUIListEntry _selectedTransformUIEntry;
        private static ComponentUtilUI.GenericUIListEntry _selectedComponentUiEntry;
        // selected reference type in ComponentInspector
        private static ComponentUtilUI.PropertyUIEntry _selectedReferencePropertyUiEntry;
        #endregion currently selected

        /// <summary>
        /// the property and field types ComponentUtil supports
        /// </summary>
        public static readonly HashSet<Type> supportedTypes =
        [
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(bool),
            typeof(int),
            typeof(uint),
            typeof(short),
            typeof(ushort),
            typeof(long),
            typeof(ulong),
            typeof(byte),
            typeof(sbyte),
            typeof(nint),
            typeof(nuint),
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Quaternion),
            typeof(Color),
            typeof(string),
        ];

        /// <summary>
        /// the property and field types ComponentUtil supports that should be treated as reference types
        /// </summary>
        public static readonly HashSet<Type> supportedTypesRewireAsReference =
        [
            typeof(ParticleSystem.MainModule),
            typeof(ParticleSystem.CollisionModule),
            typeof(ParticleSystem.ColorBySpeedModule),
            typeof(ParticleSystem.ColorOverLifetimeModule),
            typeof(ParticleSystem.CustomDataModule),
            typeof(ParticleSystem.EmissionModule),
            typeof(ParticleSystem.ExternalForcesModule),
            typeof(ParticleSystem.ForceOverLifetimeModule),
            typeof(ParticleSystem.InheritVelocityModule),
            typeof(ParticleSystem.LightsModule),
            typeof(ParticleSystem.LimitVelocityOverLifetimeModule),
            typeof(ParticleSystem.NoiseModule),
            typeof(ParticleSystem.RotationBySpeedModule),
            typeof(ParticleSystem.RotationOverLifetimeModule),
            typeof(ParticleSystem.ShapeModule),
            typeof(ParticleSystem.SizeBySpeedModule),
            typeof(ParticleSystem.SizeOverLifetimeModule),
            typeof(ParticleSystem.SubEmittersModule),
            typeof(ParticleSystem.TextureSheetAnimationModule),
            typeof(ParticleSystem.TrailModule),
            typeof(ParticleSystem.TriggerModule),
            typeof(ParticleSystem.VelocityOverLifetimeModule),
        ];

        /// <summary>
        /// the property and field types ComponentUtil explicitly does not support (blacklist)
        /// </summary>
        public static readonly HashSet<Type> blacklistTypes = [ ];

        /// <summary>
        /// sets selected objects to null, resets the tracker, UI pools, cache and pages
        /// </summary>
        public void ResetState()
        {
            _selectedGO = null;
            _selectedComponent = null;
            _selectedObject = null;
            _selectedReferencePropertyUiEntry = null;

            _currentPageTransformList = 0;
            _currentPageComponentList = 0;
            _currentPageComponentAdderList = 0;

            ClearTracker();
            ComponentUtilUI.ResetPageNumbers();
            ComponentUtilUI.ClearAllEntryPools();
            ComponentUtilCache.ClearCache();
        }

        /// <summary>
        /// entry point for the core functionality, flattens transform hierarchy, 
        /// lists all components, lists all properties, lists all addable components
        /// </summary>
        /// <param name="input">selected item/object to traverse</param>
        public void Entry(Studio.ObjectCtrlInfo input)
        {
            if (input == null)
                return;

            _currentPageTransformList = 0;
            ComponentUtilUI.ResetPageNumberTransform();

            _selectedObject = input;
            FlattenTransformHierarchy(input.guideObject.transformTarget.gameObject);
            GetAllComponents(_selectedGO, _selectedTransformUIEntry);
            GetAllComponentsAdder(_selectedGO, _selectedTransformUIEntry);
            GetAllFieldsAndProperties(_selectedComponent, _selectedComponentUiEntry);

            ComponentUtilUI.TraverseAndSetEditedParents();
        }
    }
}
