using System;
using UnityEngine;

using RSkoi_ComponentUtil.UI;
using RSkoi_ComponentUtil.Core;

namespace RSkoi_ComponentUtil
{
    public partial class ComponentUtil
    {
        internal static int _currentPageComponentAdderList = 0;

        /// <summary>
        /// Name of an addable component type the adder search matches against
        /// </summary>
        static string ComponentAdderSearchName(Type t) => t.FullName;

        /// <summary>
        /// Gets all components you could theoretically add to selected GameObject
        /// </summary>
        /// <param name="input">selected object to add components on</param>
        /// <param name="inputUi">selected transform ui entry</param>
        internal void GetAllComponentsAdder(GameObject input, ComponentUtilUI.GenericUIListEntry inputUi)
        {
            if (input == null || inputUi == null)
                return;

            ComponentUtilUI.UpdateUISelectedText(ComponentUtilUI._componentAdderListSelectedGOText, input.name);

            var componentList = Paging.GetPage(
                ComponentUtilCache.GetOrCacheComponentAdders(),
                ComponentUtilUI.PageSearchComponentAdderInputValue,
                ComponentAdderSearchName,
                _currentPageComponentAdderList,
                ItemsPerPageValue);

            ComponentUtilUI.PrepareComponentAdderPool(componentList.Count);
            for (var poolIndex = 0; poolIndex < componentList.Count; poolIndex++)
            {
                var t = componentList[poolIndex];

                var uiEntry = ComponentUtilUI.ComponentAdderListEntries[poolIndex];
                uiEntry.EntryName.text = t.Name;
                // remove all (non-persistent) listeners
                uiEntry.SelfButton.onClick.RemoveAllListeners();
                uiEntry.SelfButton.onClick.AddListener(() =>
                {
                    if (input.GetComponent(t) != null)
                    {
                        _logger.LogWarning($"Cannot add component {t.Name} to {input.name} because instance already exists");
                        return;
                    }

                    input.AddComponent(t);
                    AddComponentToTracker(_selectedObject, input, t.FullName);

                    // force refresh the component list cache
                    ComponentUtilCache.GetOrCacheComponents(input, forceRefresh: true);
                    GetAllComponents(_selectedGO, _selectedTransformUIEntry, setsSelected: false);

                    ComponentUtilUI.TraverseAndSetEditedParents();
                });
                uiEntry.UiTarget = t;
                uiEntry.ResetBg();
                uiEntry.UiGO.SetActive(true);
            }
        }

        #region filter
        internal void OnFilterComponentAdder()
        {
            if (_selectedGO == null)
                return;

            _currentPageComponentAdderList = 0;
            ComponentUtilUI.ResetPageNumberComponentAdder();

            GetAllComponentsAdder(_selectedGO, _selectedTransformUIEntry);

            ComponentUtilUI.TraverseAndSetEditedParents();
        }
        #endregion filter

        #region pages
        internal void OnLastComponentAdderPage()
        {
            if (_selectedGO == null)
                return;
            if (_currentPageComponentAdderList == 0)
                return;

            _currentPageComponentAdderList--;
            ComponentUtilUI.UpdatePageNumberComponentAdder(_currentPageComponentAdderList);
            GetAllComponentsAdder(_selectedGO, _selectedTransformUIEntry);
            ComponentUtilUI.TraverseAndSetEditedParents();
        }

        internal void OnNextComponentAdderPage()
        {
            if (_selectedGO == null)
                return;
            // page switch can only occur after the assemblies have been scanned for components
            if (!Paging.PageExists(
                ComponentUtilCache._componentAdderSearchCache.Values,
                ComponentUtilUI.PageSearchComponentAdderInputValue,
                ComponentAdderSearchName,
                _currentPageComponentAdderList + 1,
                ItemsPerPageValue))
                return;

            _currentPageComponentAdderList++;
            ComponentUtilUI.UpdatePageNumberComponentAdder(_currentPageComponentAdderList);
            GetAllComponentsAdder(_selectedGO, _selectedTransformUIEntry);
            ComponentUtilUI.TraverseAndSetEditedParents();
        }
        #endregion pages
    }
}
