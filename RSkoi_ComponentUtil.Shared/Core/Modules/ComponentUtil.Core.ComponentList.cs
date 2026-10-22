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
        /// Name of a component in the ComponentList, used for display and search
        /// </summary>
        static string ComponentListName(Component c) => c.GetType().Name;

        /// <summary>
        /// Gets all components on currently selected transform, maps to list entries, selects first list entry
        /// </summary>
        /// <param name="input">selected object to find all components in</param>
        /// <param name="inputUi">selected transform ui entry</param>
        /// <param name="setsSelected">whether _selectedComponent and _selectedComponentUiEntry should be set to first item in component list</param>
        internal void GetAllComponents(GameObject input, ComponentUtilUI.GenericUIListEntry inputUi, bool setsSelected = true)
        {
            if (input == null || inputUi == null)
                return;

            ComponentUtilUI.UpdateUISelectedText(ComponentUtilUI._componentListSelectedGOText, input.name);

            var componentList = Paging.GetPage(
                ComponentUtilCache.GetOrCacheComponents(input),
                ComponentUtilUI.PageSearchComponentListInputValue,
                ComponentListName,
                _currentPageComponentList,
                ItemsPerPageValue);

            ComponentUtilUI.PrepareComponentPool(componentList.Count);
            for (var poolIndex = 0; poolIndex < componentList.Count; poolIndex++)
            {
                var c = componentList[poolIndex];

                var uiEntry = ComponentUtilUI.ComponentListEntries[poolIndex];
                uiEntry.EntryName.text = ComponentListName(c);
                // remove all (non-persistent) listeners
                uiEntry.SelfButton.onClick.RemoveAllListeners();
                uiEntry.SelfButton.onClick.AddListener(() => ChangeSelectedComponent(c, uiEntry));
                uiEntry.ParentUiEntry = inputUi;
                uiEntry.UiTarget = c;
                uiEntry.ResetBg();
                uiEntry.UiGO.SetActive(true);
            }

            SetSelectedComponent(setsSelected, componentList);
        }

        void ChangeSelectedComponent(Component target, ComponentUtilUI.GenericUIListEntry uiEntry)
        {
            _selectedComponent = target;
            _selectedComponentUiEntry = uiEntry;

            uiEntry.ResetBg();

            GetAllFieldsAndProperties(_selectedComponent, _selectedComponentUiEntry);

            ComponentUtilUI.TraverseAndSetEditedParents();
        }

        #region filter
        internal void OnFilterComponent()
        {
            if (_selectedGO == null)
                return;

            _currentPageComponentList = 0;
            ComponentUtilUI.ResetPageNumberComponentList();

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
            ComponentUtilUI.UpdatePageNumberComponentList(_currentPageComponentList);
            GetAllComponents(_selectedGO, _selectedTransformUIEntry, setsSelected: false);
            ComponentUtilUI.TraverseAndSetEditedParents();
        }

        internal void OnNextComponentPage()
        {
            if (_selectedGO == null)
                return;
            if (!Paging.PageExists(
                ComponentUtilCache.GetOrCacheComponents(_selectedGO),
                ComponentUtilUI.PageSearchComponentListInputValue,
                ComponentListName,
                _currentPageComponentList + 1,
                ItemsPerPageValue))
                return;

            _currentPageComponentList++;
            ComponentUtilUI.UpdatePageNumberComponentList(_currentPageComponentList);
            GetAllComponents(_selectedGO, _selectedTransformUIEntry, setsSelected: false);
            ComponentUtilUI.TraverseAndSetEditedParents();
        }
        #endregion pages

        #region setter, getter
        void SetSelectedComponent(bool setsSelected, List<Component> cacheList)
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
