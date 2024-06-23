using UnityEngine;
using UnityEngine.UI;

namespace RSkoi_ComponentUtil.UI
{
    internal static partial class ComponentUtilUI
    {
        // overarching window container
        internal static Transform _componentAdderWindow;
        internal static RectTransform _componentAdderWindowRect;
        internal static Vector2 _componentAdderWindowRectOriginalSize;
        // scroll view content container
        internal static Transform _componentAdderListContainer;
        // specific to ComponentAdder window
        internal static Text _componentAdderListSelectedGOText;

        // pages
        private static InputField _pageSearchComponentAdderInput;
        internal static string PageSearchComponentAdderInputValue
        {
            get
            {
                if (_pageSearchComponentAdderInput == null)
                    return "";
                return _pageSearchComponentAdderInput.text;
            }
        }
        private static Text _currentPageComponentAdderText;
        private static Button _pageLastComponentAdderButton;
        private static Button _pageNextComponentAdderButton;

        internal static void UpdatePageNumberComponentAdder(int pageNumber)
        {
            // +1 because pages are zero-indexed
            _currentPageComponentAdderText.text = (pageNumber + 1).ToString();
        }

        internal static void ResetPageNumberComponentAdder()
        {
            _currentPageComponentAdderText.text = "1";
        }
    }
}
