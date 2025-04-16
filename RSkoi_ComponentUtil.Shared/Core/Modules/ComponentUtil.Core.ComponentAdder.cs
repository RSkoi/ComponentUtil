using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using RSkoi_ComponentUtil.UI;
using RSkoi_ComponentUtil.Core;

namespace RSkoi_ComponentUtil
{
    public partial class ComponentUtil
    {
        internal static int _currentPageComponentAdderList = 0;

        /// <summary>
        /// gets all components you could theoretically add to selected GameObject
        /// </summary>
        /// <param name="input">selected object to add components on</param>
        /// <param name="inputUi">selected transform ui entry</param>
        internal void GetAllComponentsAdder(GameObject input, ComponentUtilUI.GenericUIListEntry inputUi)
        {
            if (input == null || inputUi == null)
                return;

            ComponentUtilUI.UpdateUISelectedText(ComponentUtilUI._componentAdderListSelectedGOText, input.name);

            List<Type> componentList = ComponentUtilCache.GetOrCacheComponentAdders();

            // filter string
            string filter = ComponentUtilUI.PageSearchComponentAdderInputValue.ToLower();
            if (filter != "")
                componentList = componentList.Where(t => t.FullName.ToLower().Contains(filter)).ToList();

            // paging
            int itemsPerPage = ItemsPerPageValue;
            int startIndex = _currentPageComponentAdderList * itemsPerPage;
            int n = (componentList.Count - startIndex) <= itemsPerPage ? componentList.Count - startIndex : itemsPerPage;

            if (componentList.Count != 0)
                componentList = componentList.GetRange(startIndex, n);

            ComponentUtilUI.PrepareComponentAdderPool(componentList.Count);
            for (int poolIndex = 0; poolIndex < componentList.Count; poolIndex++)
            {
                Type t = componentList[poolIndex];

                ComponentUtilUI.GenericUIListEntry uiEntry = ComponentUtilUI.ComponentAdderListEntries[poolIndex];
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
                    ComponentUtilCache.GetOrCacheComponents(input, true);
                    GetAllComponents(_selectedGO, _selectedTransformUIEntry, false);

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
            int toBeStartIndex = (_currentPageComponentAdderList + 1) * ItemsPerPageValue;
            // page switch can only occur after the assemblies have been scanned for components
            var cached = ComponentUtilCache._componentAdderSearchCache.Values;
            // out of bounds start index
            if (toBeStartIndex >= cached.Count)
                return;

            // if filter string reduces length of transform list
            string filter = ComponentUtilUI.PageSearchComponentAdderInputValue.ToLower();
            if (filter != "" && (toBeStartIndex >= cached
                .Where(t => t.Name.ToLower().Contains(filter))
                .Count()))
                return;

            _currentPageComponentAdderList++;
            ComponentUtilUI.UpdatePageNumberComponentAdder(_currentPageComponentAdderList);
            GetAllComponentsAdder(_selectedGO, _selectedTransformUIEntry);
            ComponentUtilUI.TraverseAndSetEditedParents();
        }
        #endregion pages
    }
}
