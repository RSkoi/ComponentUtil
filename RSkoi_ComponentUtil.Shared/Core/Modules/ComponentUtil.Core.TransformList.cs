using System.Collections.Generic;
using UnityEngine;
using Studio;

using RSkoi_ComponentUtil.UI;
using RSkoi_ComponentUtil.Core;

namespace RSkoi_ComponentUtil
{
    public partial class ComponentUtil
    {
        internal static int _currentPageTransformList = 0;

        /// <summary>
        /// Name of a transform in the TransformList, used for display and search
        /// </summary>
        static string TransformListName(Transform t) => t.name;

        /// <summary>
        /// Flattens transform hierarchy of input to list entries, selects first list entry
        /// </summary>
        /// <param name="inputCtrl">selected item/object to traverse</param>
        /// <param name="setsSelected">whether _selectedGO and _selectedTransformUIEntry should be set to first item in flattened transform hiararchy<br/>
        /// also whether to reset current component page number</param>
        internal void FlattenTransformHierarchy(ObjectCtrlInfo inputCtrl, bool setsSelected = true)
        {
            if (inputCtrl == null)
                return;

            var transformList = Paging.GetPage(
                ComponentUtilCache.GetOrCacheTransforms(inputCtrl),
                ComponentUtilUI.PageSearchTransformListInputValue,
                TransformListName,
                _currentPageTransformList,
                ItemsPerPageValue);

            ComponentUtilUI.PrepareTransformPool(transformList.Count);
            for (var poolIndex = 0; poolIndex < transformList.Count; poolIndex++)
            {
                var t = transformList[poolIndex];

                var uiEntry = ComponentUtilUI.TransformListEntries[poolIndex];
                uiEntry.EntryName.text = TransformListName(t);
                // remove all (non-persistent) listeners
                uiEntry.SelfButton.onClick.RemoveAllListeners();
                uiEntry.SelfButton.onClick.AddListener(() => ChangeSelectedGO(t.gameObject, uiEntry));
                // transform ui entries have no parent entry
                uiEntry.ParentUiEntry = null;
                uiEntry.UiTarget = t;
                uiEntry.ResetBg();
                uiEntry.UiGO.SetActive(true);
            }

            SetSelectedTransform(setsSelected, transformList);
        }

        void ChangeSelectedGO(GameObject target, ComponentUtilUI.GenericUIListEntry uiEntry)
        {
            _selectedGO = target;
            _selectedTransformUIEntry = uiEntry;

            _currentPageComponentList = 0;
            ComponentUtilUI.ResetPageNumberComponentList();

            GetAllComponents(_selectedGO, _selectedTransformUIEntry);
            GetAllComponentsAdder(_selectedGO, _selectedTransformUIEntry);
            GetAllFieldsAndProperties(_selectedComponent, _selectedComponentUiEntry);

            ComponentUtilUI.TraverseAndSetEditedParents();
        }

        #region filter
        internal void OnFilterTransform()
        {
            if (_selectedObject == null)
                return;

            // essentially does this:
            //Entry(_selectedObject);

            _currentPageTransformList = 0;
            ComponentUtilUI.UpdatePageNumberTransformList(_currentPageTransformList);

            FlattenTransformHierarchy(_selectedObject);
            GetAllComponents(_selectedGO, _selectedTransformUIEntry);
            GetAllComponentsAdder(_selectedGO, _selectedTransformUIEntry);
            GetAllFieldsAndProperties(_selectedComponent, _selectedComponentUiEntry);

            ComponentUtilUI.TraverseAndSetEditedParents();
        }
        #endregion filter

        #region pages
        internal void OnLastTransformPage()
        {
            if (_selectedObject == null)
                return;
            if (_currentPageTransformList == 0)
                return;

            _currentPageTransformList--;
            ComponentUtilUI.UpdatePageNumberTransformList(_currentPageTransformList);
            FlattenTransformHierarchy(_selectedObject, setsSelected: false);
            ComponentUtilUI.TraverseAndSetEditedParents();
        }

        internal void OnNextTransformPage()
        {
            if (_selectedObject == null)
                return;
            if (!Paging.PageExists(
                ComponentUtilCache.GetOrCacheTransforms(_selectedObject),
                ComponentUtilUI.PageSearchTransformListInputValue,
                TransformListName,
                _currentPageTransformList + 1,
                ItemsPerPageValue))
                return;

            _currentPageTransformList++;
            ComponentUtilUI.UpdatePageNumberTransformList(_currentPageTransformList);
            FlattenTransformHierarchy(_selectedObject, setsSelected: false);
            ComponentUtilUI.TraverseAndSetEditedParents();
        }
        #endregion pages

        #region setter, getter
        void SetSelectedTransform(bool setsSelected, List<Transform> cacheList)
        {
            if (!setsSelected)
                return;
            if (cacheList == null)
                return;

            _currentPageComponentList = 0;
            ComponentUtilUI.ResetPageNumberComponentList();

            if (cacheList.Count == 0)
                return;

            _selectedGO = cacheList[0].gameObject;
            _selectedTransformUIEntry = ComponentUtilUI.TransformListEntries[0];
        }
        #endregion setter, getter
    }
}
