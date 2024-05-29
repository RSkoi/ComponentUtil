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
        #endregion entry prefabs

        // overarching window container
        internal static Transform _inspectorWindow;
        // scroll view content container
        internal static Transform _componentPropertyListContainer;
        // specific to ComponentInspector window
        private static Button _hideComponentListButton;
        internal static Button _componentDeleteButton;
        internal static Text _componentPropertyListSelectedComponentText;
    }
}
