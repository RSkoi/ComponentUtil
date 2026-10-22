using UnityEngine;
using UnityEngine.UI;

namespace RSkoi_ComponentUtil.UI
{
    internal static partial class ComponentUtilUI
    {
        // overarching window container
        internal static Transform _componentWindow;
        internal static RectTransform _componentWindowRect;
        internal static Vector2 _componentWindowRectOriginalSize;
        // scroll view content container
        internal static Transform _componentListContainer;
        // specific to ComponentList window window
        static Button _hideTransformListButton;
        static Button _toggleComponentAdderButton;
        internal static Text _componentListSelectedGOText;

        // pages
        static InputField _pageSearchComponentListInput;
        internal static string PageSearchComponentListInputValue
        {
            get
            {
                if (_pageSearchComponentListInput == null)
                    return "";
                return _pageSearchComponentListInput.text;
            }
        }
        static Text _currentPageComponentListText;
        static Button _pageLastComponentListButton;
        static Button _pageNextComponentListButton;

        internal static void UpdatePageNumberComponentList(int pageNumber)
            => _currentPageComponentListText.text = (pageNumber + 1).ToString(); // +1 because pages are zero-indexed

        internal static void ResetPageNumberComponentList() => _currentPageComponentListText.text = "1";
    }
}
