using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using RSkoi_ComponentUtil.UI;
using RSkoi_ComponentUtil.Core;

namespace RSkoi_ComponentUtil
{
    public partial class ComponentUtil
    {
        internal static int _currentPageComponentList = 0;

        /// <summary>
        /// gets all components on currently selected transform, maps to list entries
        /// , selects first list entry
        /// </summary>
        /// <param name="input">selected object to find all components in</param>
        /// <param name="inputUi">selected transform ui entry</param>
        /// <param name="setsSelected">whether _selectedComponent and _selectedComponentUiEntry
        /// should be set to first item in component list</param>
        internal void GetAllComponents(GameObject input, ComponentUtilUI.GenericUIListEntry inputUi, bool setsSelected = true)
        {
            if (input == null || inputUi == null)
                return;

            ComponentUtilUI.UpdateUISelectedText(ComponentUtilUI._componentListSelectedGOText, input.name);

            List<Component> list = [.. ComponentUtilCache.GetOrCacheComponents(input)];

            // filter string
            string filter = ComponentUtilUI.PageSearchComponentInputValue.ToLower();
            if (filter != "")
                list = list.Where(c => c.GetType().Name.ToLower().Contains(filter)).ToList();

            // paging
            int itemsPerPage = ItemsPerPageValue;
            int startIndex = _currentPageComponentList * itemsPerPage;
            int n = (list.Count - startIndex) <= itemsPerPage ? list.Count - startIndex : itemsPerPage;

            if (list.Count != 0)
                list = list.GetRange(startIndex, n);

            ComponentUtilUI.PrepareComponentPool(list.Count);
            for (int poolIndex = 0; poolIndex < list.Count; poolIndex++)
            {
                Component c = list[poolIndex];

                ComponentUtilUI.GenericUIListEntry uiEntry = ComponentUtilUI.ComponentListEntries[poolIndex];
                uiEntry.EntryName.text = c.GetType().Name;
                // remove all (non-persistent) listeners
                uiEntry.SelfButton.onClick.RemoveAllListeners();
                uiEntry.SelfButton.onClick.AddListener(() => ChangeSelectedComponent(c, uiEntry));
                uiEntry.ParentUiEntry = inputUi;
                uiEntry.UiTarget = c;
                uiEntry.ResetBgAndChildren();
                uiEntry.UiGO.SetActive(true);
            }

            SetSelectedComponent(setsSelected, list);
        }

        private void ChangeSelectedComponent(Component target, ComponentUtilUI.GenericUIListEntry uiEntry)
        {
            _selectedComponent = target;
            _selectedComponentUiEntry = uiEntry;

            uiEntry.ResetBgAndChildren();

            GetAllFieldsAndProperties(_selectedComponent, _selectedComponentUiEntry);

            ComponentUtilUI.TraverseAndSetEditedParents();
        }

        #region filter
        internal void OnFilterComponent()
        {
            if (_selectedGO == null)
                return;

            _currentPageComponentList = 0;
            ComponentUtilUI.ResetPageNumberComponent();

            GetAllComponents(_selectedGO, _selectedTransformUIEntry);
            GetAllFieldsAndProperties(_selectedComponent, _selectedComponentUiEntry);

            ComponentUtilUI.TraverseAndSetEditedParents();
        }
        #endregion filter

        #region pages
        internal void OnLastComponentPage()
        {
            if (_selectedGO == null)
                return;
            if (_currentPageComponentList == 0)
                return;

            _currentPageComponentList--;
            ComponentUtilUI.UpdatePageNumberComponent(_currentPageComponentList);
            GetAllComponents(_selectedGO, _selectedTransformUIEntry, false);
            ComponentUtilUI.TraverseAndSetEditedParents();
        }

        internal void OnNextComponentPage()
        {
            if (_selectedGO == null)
                return;
            int toBeStartIndex = (_currentPageComponentList + 1) * ItemsPerPageValue;
            // page switch can only occur after the _selectedGO has been scanned for components
            Component[] cached = ComponentUtilCache._componentSearchCache[_selectedGO];
            // out of bounds start index
            if (toBeStartIndex >= cached.Length)
                return;

            // if filter string reduces length of transform list
            string filter = ComponentUtilUI.PageSearchComponentInputValue.ToLower();
            if (filter != "" && (toBeStartIndex >= cached
                .Where(c => c.GetType().Name.ToLower().Contains(filter))
                .ToArray().Length))
                return;

            _currentPageComponentList++;
            ComponentUtilUI.UpdatePageNumberComponent(_currentPageComponentList);
            GetAllComponents(_selectedGO, _selectedTransformUIEntry, false);
            ComponentUtilUI.TraverseAndSetEditedParents();
        }
        #endregion pages

        #region setter, getter
        private void SetSelectedComponent(bool setsSelected, List<Component> cacheList)
        {
            if (!setsSelected)
                return;
            if (cacheList == null)
                return;

            if (cacheList.Count == 0)
                return;

            _selectedComponent = cacheList[0];
            _selectedComponentUiEntry = ComponentUtilUI.ComponentListEntries[0];
        }
        #endregion setter, getter
    }
}
