using System;
using UnityEngine;
using UnityEngine.UI;

namespace RSkoi_ComponentUtil.UI
{
    internal static partial class ComponentUtilUI
    {
        #region entry prefabs
        internal static GameObject _componentPropertyDecimalEntryPrefab;
        internal static GameObject _componentPropertyEnumEntryPrefab;
        internal static GameObject _componentPropertyBoolEntryPrefab;
        internal static GameObject _componentPropertyVector4EntryPrefab;
        internal static GameObject _componentPropertyReferenceEntryPrefab;
        internal static GameObject _componentPropertyColorEntryPrefab;
        internal static GameObject _componentPropertyNullEntryPrefab;
        #endregion entry prefabs

        // overarching window container
        internal static Transform _inspectorWindow;
        internal static RectTransform _inspectorWindowRect;
        internal static Vector2 _inspectorWindowRectOriginalSize;
        // scroll view content container
        internal static Transform _componentPropertyListContainer;
        // specific to ComponentInspector window
        private static Button _hideComponentListButton;
        internal static Button _componentDeleteButton;
        internal static Text _componentPropertyListSelectedComponentText;
        internal static Button _componentPropertyCopyButton;
        internal static Button _componentPropertyPasteButton;

        // pages
        private static Button _refreshComponentInspectorButton;
        private static InputField _pageSearchComponentInspectorInput;
        internal static string PageSearchComponentInspectorInputValue
        {
            get
            {
                if (_pageSearchComponentInspectorInput == null)
                    return "";
                return _pageSearchComponentInspectorInput.text;
            }
        }

        internal static GameObject MapPropertyOrFieldToEntryPrefab(Type t)
        {
            if (t == null)
                return _componentPropertyNullEntryPrefab;
            else if (t.IsEnum)
                return _componentPropertyEnumEntryPrefab;
            else if (t.Equals(typeof(bool)))
                return _componentPropertyBoolEntryPrefab;
            else if (t.Equals(typeof(Vector2)) ||
                     t.Equals(typeof(Vector3)) ||
                     t.Equals(typeof(Vector4)) ||
                     t.Equals(typeof(Quaternion)))
                return _componentPropertyVector4EntryPrefab;
            else if (t.Equals(typeof(Color)))
                return _componentPropertyColorEntryPrefab;
            else if (t.Equals(typeof(string)))
                return _componentPropertyDecimalEntryPrefab;
            else if (!t.IsValueType || ComponentUtil.supportedTypesRewireAsReference.Contains(t))
                return _componentPropertyReferenceEntryPrefab;

            return _componentPropertyDecimalEntryPrefab;
        }
    }
}
