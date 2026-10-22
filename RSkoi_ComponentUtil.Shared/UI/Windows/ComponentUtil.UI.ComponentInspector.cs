using UnityEngine;
using UnityEngine.UI;

using RSkoi_ComponentUtil.Core;

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
        static Button _hideComponentListButton;
        internal static Button _componentDeleteButton;
        internal static Text _componentPropertyListSelectedComponentText;
        internal static Button _componentPropertyCopyButton;
        internal static Button _componentPropertyPasteButton;

        // pages
        static Button _refreshComponentInspectorButton;
        static InputField _pageSearchComponentInspectorInput;
        internal static string PageSearchComponentInspectorInputValue
        {
            get
            {
                if (_pageSearchComponentInspectorInput == null)
                    return "";
                return _pageSearchComponentInspectorInput.text;
            }
        }

        /// <summary>
        /// Prefab for entries of properties / fields whose value is null
        /// </summary>
        internal static GameObject NullEntryPrefab => _componentPropertyNullEntryPrefab;

        internal static GameObject MapEntryKindToPrefab(EntryKind kind) => kind switch
        {
            EntryKind.Enum      => _componentPropertyEnumEntryPrefab,
            EntryKind.Bool      => _componentPropertyBoolEntryPrefab,
            EntryKind.Vector    => _componentPropertyVector4EntryPrefab,
            EntryKind.Color     => _componentPropertyColorEntryPrefab,
            EntryKind.Reference => _componentPropertyReferenceEntryPrefab,
            _                   => _componentPropertyDecimalEntryPrefab, // input field, used for numbers and strings
        };
    }
}
