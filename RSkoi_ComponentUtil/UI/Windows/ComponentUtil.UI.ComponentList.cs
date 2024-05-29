using UnityEngine;
using UnityEngine.UI;

namespace RSkoi_ComponentUtil.UI
{
    internal static partial class ComponentUtilUI
    {
        // overarching window container
        internal static Transform _componentWindow;
        // scroll view content container
        internal static Transform _componentListContainer;
        // specific to ComponentList window window
        private static Button _hideTransformListButton;
        private static Button _toggleComponentAdderButton;
        internal static Text _componentListSelectedGOText;

        // pages
        private static InputField _pageSearchComponentInput;
        internal static string PageSearchComponentInputValue
        {
            get
            {
                if (_pageSearchComponentInput == null)
                    return "";
                return _pageSearchComponentInput.text;
            }
        }
        private static Text _currentPageComponentText;
        private static Button _pageLastComponentButton;
        private static Button _pageNextComponentButton;

        internal static void UpdatePageNumberComponent(int pageNumber)
        {
            // +1 because pages are zero-indexed
            _currentPageComponentText.text = (pageNumber + 1).ToString();
        }

        internal static void ResetPageNumberComponent()
        {
            _currentPageComponentText.text = "1";
        }
    }
}
