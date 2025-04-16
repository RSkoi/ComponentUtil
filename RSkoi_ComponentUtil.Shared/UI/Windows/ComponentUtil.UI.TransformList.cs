using UnityEngine;
using UnityEngine.UI;

namespace RSkoi_ComponentUtil.UI
{
    internal static partial class ComponentUtilUI
    {
        // overarching window container
        internal static Transform _transformWindow;
        internal static RectTransform _transformWindowRect;
        internal static Vector2 _transformWindowRectOriginalSize;
        // scroll view content container
        internal static Transform _transformListContainer;

        // pages
        private static InputField _pageSearchTransformListInput;
        internal static string PageSearchTransformListInputValue
        {
            get
            {
                if (_pageSearchTransformListInput == null)
                    return "";
                return _pageSearchTransformListInput.text;
            }
        }
        private static Text _currentPageTransformListText;
        private static Button _pageLastTransformListButton;
        private static Button _pageNextTransformListButton;

        internal static void UpdatePageNumberTransformList(int pageNumber)
        {
            // +1 because pages are zero-indexed
            _currentPageTransformListText.text = (pageNumber + 1).ToString();
        }

        internal static void ResetPageNumberTransformList()
        {
            _currentPageTransformListText.text = "1";
        }
    }
}
