using UnityEngine;
using UnityEngine.UI;

namespace RSkoi_ComponentUtil.UI
{
    internal static partial class ComponentUtilUI
    {
        // overarching window container
        internal static Transform _objectInspectorWindow;
        internal static RectTransform _objectInspectorWindowRect;
        internal static Vector2 _objectInspectorWindowRectOriginalSize;
        // scroll view content container
        internal static Transform _objectPropertyListContainer;
        // specific to ComponentInspector window
        internal static Text _objectPropertyListSelectedText;
    }
}
