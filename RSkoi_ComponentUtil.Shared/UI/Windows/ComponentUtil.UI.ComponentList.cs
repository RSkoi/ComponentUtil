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
        private static Button _hideTransformListButton;
        private static Button _toggleComponentAdderButton;
        internal static Text _componentListSelectedGOText;

        // pages
        private static InputField _pageSearchComponentListInput;
        internal static string PageSearchComponentListInputValue
        {
            get
            {
                if (_pageSearchComponentListInput == null)
                    return "";
                return _pageSearchComponentListInput.text;
            }
        }
        private static Text _currentPageComponentListText;
        private static Button _pageLastComponentListButton;
        private static Button _pageNextComponentListButton;

        internal static void UpdatePageNumberComponentList(int pageNumber)
        {
            // +1 because pages are zero-indexed
            _currentPageComponentListText.text = (pageNumber + 1).ToString();
        }

        internal static void ResetPageNumberComponentList()
        {
            _currentPageComponentListText.text = "1";
        }
    }
}
