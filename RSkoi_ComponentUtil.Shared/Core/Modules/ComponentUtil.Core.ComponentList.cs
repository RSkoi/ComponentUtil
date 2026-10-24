using UnityEngine;

using RSkoi_ComponentUtil.UI;
using RSkoi_ComponentUtil.Core;

namespace RSkoi_ComponentUtil
{
    internal static partial class ComponentUtilEditor
    {
        internal static int _currentPageComponentList = 0;

        /// <summary>
        /// Name of a component in the ComponentList, used for display and search.
        /// </summary>
        static string ComponentListName(Component c) => c.GetType().Name;

        /// <summary>
        /// Gets all components on currently selected transform, maps to list entries, selects first list entry.
        /// </summary>
        /// <param name="input">selected object to find all components in</param>
        /// <param name="setsSelected">whether _selectedComponent should be set to the first component matching the search
        /// (or the first component of input if none matches)</param>
        internal static void GetAllComponents(GameObject input, bool setsSelected = true)
        {
            if (input == null)
                return;

            ComponentUtilUI.UpdateUISelectedText(ComponentUtilUI._componentListSelectedGOText, input.name);

            var allComponents = ComponentUtilCache.GetOrCacheComponents(input);
            var componentList = Paging.GetPage(
                allComponents,
                ComponentUtilUI.PageSearchComponentListInputValue,
                ComponentListName,
                _currentPageComponentList,
                ComponentUtil.ItemsPerPageValue);

            ComponentUtilUI.PrepareComponentPool(componentList.Count);
            for (var poolIndex = 0; poolIndex < componentList.Count; poolIndex++)
            {
                var c = componentList[poolIndex];

                var uiEntry = ComponentUtilUI.ComponentListEntries[poolIndex];
                uiEntry.EntryName.text = ComponentListName(c);
                // remove all (non-persistent) listeners
                uiEntry.SelfButton.onClick.RemoveAllListeners();
                uiEntry.SelfButton.onClick.AddListener(() => ChangeSelectedComponent(c));
                uiEntry.UiTarget = c;
                uiEntry.SetBgColorDefault();
                uiEntry.UiGO.SetActive(true);
            }

            SetSelectedComponent(setsSelected, allComponents);
        }

        static void ChangeSelectedComponent(Component target)
        {
            _selectedComponent = target;

            GetAllFieldsAndProperties(_selectedComponent);

            ComponentUtilUI.TraverseAndSetEditedParents();
        }

        #region filter
        internal static void OnFilterComponent()
        {
            if (_selectedGO == null)
                return;

            _currentPageComponentList = 0;
            ComponentUtilUI.ResetPageNumberComponentList();

            GetAllComponents(_selectedGO, setsSelected: false);

            ComponentUtilUI.TraverseAndSetEditedParents();
        }
        #endregion filter

        #region pages
        internal static void OnLastComponentPage()
        {
            if (_selectedGO == null)
                return;
            if (_currentPageComponentList == 0)
                return;

            _currentPageComponentList--;
            ComponentUtilUI.UpdatePageNumberComponentList(_currentPageComponentList);
            GetAllComponents(_selectedGO, setsSelected: false);
            ComponentUtilUI.TraverseAndSetEditedParents();
        }

        internal static void OnNextComponentPage()
        {
            if (_selectedGO == null)
                return;
            if (!Paging.PageExists(
                ComponentUtilCache.GetOrCacheComponents(_selectedGO),
                ComponentUtilUI.PageSearchComponentListInputValue,
                ComponentListName,
                _currentPageComponentList + 1,
                ComponentUtil.ItemsPerPageValue))
                return;

            _currentPageComponentList++;
            ComponentUtilUI.UpdatePageNumberComponentList(_currentPageComponentList);
            GetAllComponents(_selectedGO, setsSelected: false);
            ComponentUtilUI.TraverseAndSetEditedParents();
        }
        #endregion pages

        #region setter, getter
        static void SetSelectedComponent(bool setsSelected, Component[] allComponents)
        {
            if (!setsSelected || allComponents == null || allComponents.Length == 0)
                return;

            _selectedComponent = Paging.FirstMatchOrFirst(allComponents, ComponentUtilUI.PageSearchComponentListInputValue, ComponentListName);
        }
        #endregion setter, getter
    }
}
