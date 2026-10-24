using RSkoi_ComponentUtil.Core;
using RSkoi_ComponentUtil.Timeline;
using RSkoi_ComponentUtil.UI;
using System.Collections.Generic;
using UnityEngine;
using static RSkoi_ComponentUtil.ComponentUtilTracker;

namespace RSkoi_ComponentUtil
{
    /// <summary>
    /// Editing logic behind the ComponentUtil windows: what is selected,
    /// what happens when user selects, edits, copies or pastes something.<br/>
    /// Class is split into multiple files:<br/>
    /// - selection and entry point (this file)<br/>
    /// - modules (logic related to windows)
    /// </summary>
    internal static partial class ComponentUtilEditor
    {
        #region currently selected
        /// <summary>
        /// Selected ObjectCtrlInfo in workspace.
        /// </summary>
        internal static Studio.ObjectCtrlInfo _selectedObject;
        /// <summary>
        /// Selected GameObject in TransformList.
        /// </summary>
        internal static GameObject _selectedGO;
        /// <summary>
        /// Selected Component in ComponentList.
        /// </summary>
        internal static Component _selectedComponent;

        /// <summary>
        /// Selected reference type / object in ComponentInspector.
        /// </summary>
        static ComponentUtilUI.PropertyUIEntry _selectedReferencePropertyUiEntry;
        /// <summary>
        /// Member of selected reference type / object.
        /// </summary>
        static Member _selectedReferenceMember;
        /// <summary>
        /// Currently displayed reference entries in the component inspector.
        /// </summary>
        static readonly Dictionary<string, ComponentUtilUI.PropertyUIEntry> _shownReferenceEntries = [];
        #endregion currently selected

        #region currently copied
        /// <summary>
        /// Values of edited properties / fields with property / field name as key.
        /// </summary>
        internal static CopiedEdits _currentCopiedEdits;
        #endregion currently copied

        #region keys selected item
        /// <summary>
        /// Tracker key for edits of a component of the selected item.
        /// </summary>
        internal static PropertyKey SelectedPropertyKey(Component component)
            => new(_selectedObject, component.gameObject, component);

        /// <summary>
        /// Tracker key for edits inside a reference type of a component of the selected item.
        /// </summary>
        internal static PropertyReferenceKey SelectedPropertyReferenceKey(Component component, string referencePropertyName)
            => new(_selectedObject, component.gameObject, component, referencePropertyName);
        #endregion keys selected item

        /// <summary>
        /// Sets selected objects to null, resets the tracker, UI pools, cache, edit copies and pages.
        /// </summary>
        internal static void ResetState()
        {
            _selectedGO = null;
            _selectedComponent = null;
            _selectedObject = null;
            _selectedReferencePropertyUiEntry = null;
            _currentCopiedEdits = null;

            _shownReferenceEntries.Clear();

            if (ComponentUtilUI._componentPropertyPasteButton != null) // the UI may not be set up yet
                ComponentUtilUI._componentPropertyPasteButton.interactable = false;
            if (ComponentUtilUI._objectPropertyPasteButton != null)
                ComponentUtilUI._objectPropertyPasteButton.interactable = false;

            _currentPageTransformList = 0;
            _currentPageComponentList = 0;
            _currentPageComponentAdderList = 0;

            ClearTracker();
            ComponentUtilUI.ResetPageNumbers();
            ComponentUtilUI.ClearAllEntryPools();
            ComponentUtilCache.ClearCache();
            ComponentUtilTimeline.ResetState();
        }

        /// <summary>
        /// Entry point for the core functionality, flattens transform hierarchy,
        /// lists all components, lists all properties, lists all addable components.
        /// </summary>
        /// <param name="input">selected item/object to traverse</param>
        internal static void Entry(Studio.ObjectCtrlInfo input)
        {
            if (input == null)
                return;

            _currentPageTransformList = 0;
            ComponentUtilUI.ResetPageNumberTransformList();

            _selectedObject = input;
            FlattenTransformHierarchy(_selectedObject);
            GetAllComponents(_selectedGO);
            GetAllComponentsAdder(_selectedGO);
            GetAllFieldsAndProperties(_selectedComponent);

            ComponentUtilUI.TraverseAndSetEditedParents();
        }
    }
}
