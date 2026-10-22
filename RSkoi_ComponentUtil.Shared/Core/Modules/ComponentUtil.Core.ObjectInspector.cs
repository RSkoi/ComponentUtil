using System;
using UnityEngine;
using UnityEngine.UI;

using RSkoi_ComponentUtil.UI;
using RSkoi_ComponentUtil.Core;

namespace RSkoi_ComponentUtil
{
    public partial class ComponentUtil
    {
        internal void OpenObjectInspector(Member member, object value,
            ComponentUtilUI.PropertyUIEntry uiEntry, ComponentUtilUI.GenericUIListEntry parentUiEntry)
        {
            uiEntry.Wrapper = value;
            _selectedReferencePropertyUiEntry = uiEntry;

            var colorPalette = Studio.Studio.Instance.colorPalette;
            if (colorPalette.visible)           // this closes the palette even if it doesn't 'belong' to ComponentUtil
                colorPalette.visible = false;   // fix would be to e.g. check title of the window but that means a dependency

            if (!ComponentUtilUI._objectInspectorWindow.gameObject.activeSelf)
                ComponentUtilUI._objectInspectorWindow.gameObject.SetActive(true);

            GetAllFieldsAndPropertiesOfObject(member.Name, member.MemberType, value, parentUiEntry);
        }

        /// <summary>
        /// sets up object inspector: gets all fields of given object, maps to list entries of different types
        /// </summary>
        /// <param name="input">referenced object to list the properties and fields of</param>
        /// <param name="inputUi">selected component ui entry</param>
        void GetAllFieldsAndPropertiesOfObject(string propName, Type type, object input, ComponentUtilUI.GenericUIListEntry inputUi)
        {
            if (input == null || inputUi == null)
                return;

            ComponentUtilUI.ResetAndDisablePropertyEntries(objectMode: true);

            var cParent = (Component)inputUi.UiTarget;
            ComponentUtilUI.UpdateUISelectedText(
                ComponentUtilUI._objectPropertyListSelectedText,
                $"{cParent.gameObject.name}.{cParent.GetType().Name}.{propName} ({type.Name})");

            var filter = ComponentUtilUI.PageSearchObjectInspectorInputValue.ToLower();
            foreach (var p in ComponentUtilCache.GetOrCachePropertyInfosObject(input))
            {
                // ignore properties with private getters
                if (p.GetGetMethod() == null)
                    continue;

                // no recursing in the object inspector else it could lead to circular hell
                // this includes collections
                if (!p.PropertyType.IsValueType && !supportedTypes.Contains(p.PropertyType))
                    continue;

                // filter string
                if (!string.IsNullOrEmpty(filter) && !p.Name.ToLower().Contains(filter))
                    continue;

                ConfigurePropertyEntry(_selectedComponentUiEntry, input, Member.Of(p), objectMode: true);
            }

            foreach (var f in ComponentUtilCache.GetOrCacheFieldInfosObject(input))
            {
                // no recursing in the object inspector else it could lead to circular hell
                // this includes collections
                if (!f.FieldType.IsValueType && !f.FieldType.Equals(typeof(string)))
                    continue;

                // filter string
                if (!string.IsNullOrEmpty(filter) && !f.Name.ToLower().Contains(filter))
                    continue;

                ConfigurePropertyEntry(_selectedComponentUiEntry, input, Member.Of(f), objectMode: true);
            }
        }

        #region pages
        internal void OnFilterObjectInspector()
        {
            if (_selectedReferencePropertyUiEntry == null)
                return;

            ((Button)_selectedReferencePropertyUiEntry.UiSelectable).onClick.Invoke();

            ComponentUtilUI.TraverseAndSetEditedParents();
        }

        internal void OnRefreshObjectInspector()
        {
            if (_selectedReferencePropertyUiEntry == null)
                return;

            var input = _selectedReferencePropertyUiEntry.Wrapper;
            ComponentUtilCache.GetOrCachePropertyInfosObject(input, forceRefresh: true);
            ComponentUtilCache.GetOrCacheFieldInfosObject(input, forceRefresh: true);
            ((Button)_selectedReferencePropertyUiEntry.UiSelectable).onClick.Invoke();

            _logger.LogMessage("Force refreshed ObjectInspector cache");

            ComponentUtilUI.TraverseAndSetEditedParents();
        }
        #endregion pages
    }
}
