using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using RSkoi_ComponentUtil.UI;
using RSkoi_ComponentUtil.Core;

namespace RSkoi_ComponentUtil
{
    public partial class ComponentUtil
    {
        internal static int _currentPageTransformList = 0;

        /// <summary>
        /// flattens transform hierarchy of input to list entries
        /// , selects first list entry
        /// </summary>
        /// <param name="input">selected item/object to traverse</param>
        /// <param name="setsSelected">whether _selectedGO and _selectedTransformUIEntry
        /// should be set to first item in flattened transform hiararchy
        /// ; also whether to reset current component page number</param>
        internal void FlattenTransformHierarchy(GameObject input, bool setsSelected = true)
        {
            if (input == null)
                return;

            List<Transform> transformList = [.. ComponentUtilCache.GetOrCacheTransforms(input)];

            // filter string
            string filter = ComponentUtilUI.PageSearchTransformListInputValue.ToLower();
            if (filter != "")
                transformList = transformList.Where(t => t.name.ToLower().Contains(filter)).ToList();

            // paging
            int itemsPerPage = ItemsPerPageValue;
            int startIndex = _currentPageTransformList * itemsPerPage;
            int n = (transformList.Count - startIndex) <= itemsPerPage ? transformList.Count - startIndex : itemsPerPage;
            if (transformList.Count != 0)
                transformList = transformList.GetRange(startIndex, n);

            ComponentUtilUI.PrepareTransformPool(transformList.Count);
            for (int poolIndex = 0; poolIndex < transformList.Count; poolIndex++)
            {
                Transform t = transformList[poolIndex];

                ComponentUtilUI.GenericUIListEntry uiEntry = ComponentUtilUI.TransformListEntries[poolIndex];
                uiEntry.EntryName.text = t.name;
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

        private void ChangeSelectedGO(GameObject target, ComponentUtilUI.GenericUIListEntry uiEntry)
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
            
            FlattenTransformHierarchy(_selectedObject.guideObject.transformTarget.gameObject);
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
            FlattenTransformHierarchy(_selectedObject.guideObject.transformTarget.gameObject, false);
            ComponentUtilUI.TraverseAndSetEditedParents();
        }

        internal void OnNextTransformPage()
        {
            if (_selectedObject == null)
                return;
            int toBeStartIndex = (_currentPageTransformList + 1) * ItemsPerPageValue;
            // page switch can only occur after the _selectedObject has been scanned for transforms
            Transform[] cached = ComponentUtilCache._transformSearchCache[_selectedObject.guideObject.transformTarget.gameObject];
            // out of bounds start index
            if (toBeStartIndex >= cached.Length)
                return;

            // if filter string reduces length of transform list
            string filter = ComponentUtilUI.PageSearchTransformListInputValue.ToLower();
            if (filter != "" && (toBeStartIndex >= cached
                .Where(t => t.name.ToLower().Contains(filter))
                .ToArray().Length))
                return;

            _currentPageTransformList++;
            ComponentUtilUI.UpdatePageNumberTransformList(_currentPageTransformList);
            FlattenTransformHierarchy(_selectedObject.guideObject.transformTarget.gameObject, false);
            ComponentUtilUI.TraverseAndSetEditedParents();
        }
        #endregion pages

        #region setter, getter
        private void SetSelectedTransform(bool setsSelected, List<Transform> cacheList)
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
