using System;
using UnityEngine.UI;

using RSkoi_ComponentUtil.UI;
using RSkoi_ComponentUtil.Core;

using static RSkoi_ComponentUtil.SupportedTypes;

namespace RSkoi_ComponentUtil
{
    internal static partial class ComponentUtilEditor
    {
        static bool ObjectInspectorIsOpen => _selectedReferencePropertyUiEntry != null && ComponentUtilUI._objectInspectorWindow.gameObject.activeSelf;
        static void RebuildObjectInspector() => OpenObjectInspector(_selectedReferenceMember, _selectedReferencePropertyUiEntry.Wrapper, _selectedReferencePropertyUiEntry);

        internal static void OnReferenceClicked(Member member, object value, ComponentUtilUI.PropertyUIEntry uiEntry)
        {
            if (ObjectInspectorIsOpen && _selectedReferencePropertyUiEntry == uiEntry)
            {
                ComponentUtilUI._objectInspectorWindow.gameObject.SetActive(false);
                return;
            }

            OpenObjectInspector(member, value, uiEntry);
        }

        internal static void OpenObjectInspector(Member member, object value, ComponentUtilUI.PropertyUIEntry uiEntry)
        {
            uiEntry.Wrapper = value;
            _selectedReferencePropertyUiEntry = uiEntry;
            _selectedReferenceMember = member;

            var colorPalette = Studio.Studio.Instance.colorPalette;
            if (colorPalette.visible)           // this closes the palette even if it doesn't 'belong' to ComponentUtil
                colorPalette.visible = false;   // fix would be to e.g. check title of the window but that means a dependency

            if (!ComponentUtilUI._objectInspectorWindow.gameObject.activeSelf)
                ComponentUtilUI._objectInspectorWindow.gameObject.SetActive(true);

            GetAllFieldsAndPropertiesOfObject(member.Name, member.MemberType, value);
        }

        /// <summary>
        /// Sets up object inspector: gets all fields of given object, maps to list entries.
        /// </summary>
        /// <param name="input">referenced object to list the properties and fields of</param>
        static void GetAllFieldsAndPropertiesOfObject(string propName, Type type, object input)
        {
            if (input == null || _selectedComponent == null)
                return;

            ComponentUtilUI.ResetAndDisablePropertyEntries(objectMode: true);

            var cParent = _selectedComponent;
            ComponentUtilUI.UpdateUISelectedText(
                ComponentUtilUI._objectPropertyListSelectedText,
                $"{cParent.gameObject.name}.{cParent.GetType().Name}.{propName} ({type.Name})");

            var filter = ComponentUtilUI.PageSearchObjectInspectorInputValue.ToLower();
            foreach (var p in ComponentUtilCache.GetOrCachePropertyInfos(input))
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

                ConfigurePropertyEntry(input, Member.Of(p), objectMode: true);
            }

            foreach (var f in ComponentUtilCache.GetOrCacheFieldInfos(input))
            {
                // no recursing in the object inspector else it could lead to circular hell
                // this includes collections
                if (!f.FieldType.IsValueType && !f.FieldType.Equals(typeof(string)))
                    continue;

                // filter string
                if (!string.IsNullOrEmpty(filter) && !f.Name.ToLower().Contains(filter))
                    continue;

                ConfigurePropertyEntry(input, Member.Of(f), objectMode: true);
            }
        }

        #region pages
        internal static void OnFilterObjectInspector()
        {
            if (_selectedReferencePropertyUiEntry == null)
                return;

            RebuildObjectInspector();

            ComponentUtilUI.TraverseAndSetEditedParents();
        }

        internal static void OnRefreshObjectInspector()
        {
            if (_selectedReferencePropertyUiEntry == null)
                return;

            var input = _selectedReferencePropertyUiEntry.Wrapper;
            ComponentUtilCache.GetOrCachePropertyInfos(input, forceRefresh: true);
            ComponentUtilCache.GetOrCacheFieldInfos(input, forceRefresh: true);
            RebuildObjectInspector();

            Logger.LogMessage("Force refreshed ObjectInspector cache");

            ComponentUtilUI.TraverseAndSetEditedParents();
        }
        #endregion pages
    }
}
