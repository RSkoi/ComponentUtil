using UnityEngine;
using Studio;

using RSkoi_ComponentUtil.UI;
using RSkoi_ComponentUtil.Core;

namespace RSkoi_ComponentUtil
{
    internal static partial class ComponentUtilEditor
    {
        internal static int _currentPageTransformList = 0;

        /// <summary>
        /// Name of a transform in the TransformList, used for display and search.
        /// </summary>
        static string TransformListName(Transform t) => t.name;

        /// <summary>
        /// Flattens transform hierarchy of input to list entries, selects first list entry.
        /// </summary>
        /// <param name="inputCtrl">selected item/object to traverse</param>
        /// <param name="setsSelected">whether _selectedGO should be set to the first transform matching the search
        /// (or the first transform of the hierarchy if none matches); also whether to reset current component page number</param>
        internal static void FlattenTransformHierarchy(ObjectCtrlInfo inputCtrl, bool setsSelected = true)
        {
            if (inputCtrl == null)
                return;

            var allTransforms = ComponentUtilCache.GetOrCacheTransforms(inputCtrl);
            var transformList = Paging.GetPage(
                allTransforms,
                ComponentUtilUI.PageSearchTransformListInputValue,
                TransformListName,
                _currentPageTransformList,
                ComponentUtil.ItemsPerPageValue);

            ComponentUtilUI.PrepareTransformPool(transformList.Count);
            for (var poolIndex = 0; poolIndex < transformList.Count; poolIndex++)
            {
                var t = transformList[poolIndex];

                var uiEntry = ComponentUtilUI.TransformListEntries[poolIndex];
                uiEntry.EntryName.text = TransformListName(t);
                // remove all (non-persistent) listeners
                uiEntry.SelfButton.onClick.RemoveAllListeners();
                uiEntry.SelfButton.onClick.AddListener(() => ChangeSelectedGO(t.gameObject));
                uiEntry.UiTarget = t;
                uiEntry.SetBgColorDefault();
                uiEntry.UiGO.SetActive(true);
            }

            SetSelectedTransform(setsSelected, allTransforms);
        }

        static void ChangeSelectedGO(GameObject target)
        {
            _selectedGO = target;

            _currentPageComponentList = 0;
            ComponentUtilUI.ResetPageNumberComponentList();

            GetAllComponents(_selectedGO);
            GetAllComponentsAdder(_selectedGO);
            GetAllFieldsAndProperties(_selectedComponent);

            ComponentUtilUI.TraverseAndSetEditedParents();
        }

        #region filter
        internal static void OnFilterTransform()
        {
            if (_selectedObject == null)
                return;

            _currentPageTransformList = 0;
            ComponentUtilUI.UpdatePageNumberTransformList(_currentPageTransformList);

            FlattenTransformHierarchy(_selectedObject, setsSelected: false);

            ComponentUtilUI.TraverseAndSetEditedParents();
        }
        #endregion filter

        #region pages
        internal static void OnLastTransformPage()
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

        internal static void OnNextTransformPage()
        {
            if (_selectedObject == null)
                return;
            if (!Paging.PageExists(
                ComponentUtilCache.GetOrCacheTransforms(_selectedObject),
                ComponentUtilUI.PageSearchTransformListInputValue,
                TransformListName,
                _currentPageTransformList + 1,
                ComponentUtil.ItemsPerPageValue))
                return;

            _currentPageTransformList++;
            ComponentUtilUI.UpdatePageNumberTransformList(_currentPageTransformList);
            FlattenTransformHierarchy(_selectedObject, setsSelected: false);
            ComponentUtilUI.TraverseAndSetEditedParents();
        }
        #endregion pages

        #region setter, getter
        static void SetSelectedTransform(bool setsSelected, Transform[] allTransforms)
        {
            if (!setsSelected || allTransforms == null || allTransforms.Length == 0)
                return;

            _currentPageComponentList = 0;
            ComponentUtilUI.ResetPageNumberComponentList();

            var first = Paging.FirstMatchOrFirst(allTransforms, ComponentUtilUI.PageSearchTransformListInputValue, TransformListName);
            _selectedGO = first == null ? null : first.gameObject;
        }
        #endregion setter, getter
    }
}
