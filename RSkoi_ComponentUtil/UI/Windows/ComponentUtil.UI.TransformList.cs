using UnityEngine;
using UnityEngine.UI;

namespace RSkoi_ComponentUtil.UI
{
    internal static partial class ComponentUtilUI
    {
        // overarching window container
        internal static Transform _transformWindow;
        // scroll view content container
        internal static Transform _transformListContainer;

        // pages
        private static InputField _pageSearchTransformInput;
        internal static string PageSearchTransformInputValue
        {
            get
            {
                if (_pageSearchTransformInput == null)
                    return "";
                return _pageSearchTransformInput.text;
            }
        }
        private static Text _currentPageTransformText;
        private static Button _pageLastTransformButton;
        private static Button _pageNextTransformButton;

        internal static void UpdatePageNumberTransform(int pageNumber)
        {
            // +1 because pages are zero-indexed
            _currentPageTransformText.text = (pageNumber + 1).ToString();
        }

        internal static void ResetPageNumberTransform()
        {
            _currentPageTransformText.text = "1";
        }
    }
}
