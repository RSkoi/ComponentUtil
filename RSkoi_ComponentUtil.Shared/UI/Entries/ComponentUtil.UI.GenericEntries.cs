using System;
using UnityEngine;
using UnityEngine.UI;

namespace RSkoi_ComponentUtil.UI
{
    internal static partial class ComponentUtilUI
    {
        internal static GameObject _genericListEntryPrefab;

        internal static PropertyUIEntry PreConfigureNewUiEntry(GameObject entry)
        {
            var timelineButton = entry.transform.Find("TimelineButton").GetComponent<Button>();
            var resetButton = entry.transform.Find("ResetButton").GetComponent<Button>();
            var entryname = entry.transform.Find("EntryLabel").GetComponent<Text>();
            var bgImage = entry.transform.Find("EntryBg").GetComponent<Image>();
            return new(timelineButton, resetButton, entryname, bgImage, entry, uiComponentSetValueResetDelegate: null);
        }

        internal static GenericUIListEntry PreConfigureNewGenericUIListEntry(GameObject entry)
        {
            var selfButton = entry.GetComponent<Button>();
            var entryname = entry.transform.Find("EntryLabel").GetComponent<Text>();
            var bgImage = entry.transform.Find("EntryBg").GetComponent<Image>();
            return new(selfButton, entryname, bgImage, entry, uiTarget: null);
        }

        /// <summary>
        /// Generic ui list entry consisting of a button, bgImage and label.
        /// </summary>
        /// <param name="selfButton">button component of the list entry</param>
        /// <param name="entryName">label component of the list entry</param>
        /// <param name="bgImage">image component of the list entry</param>
        /// <param name="instantiatedUiGo">the instantiated GameObeject (the list entry itself)</param>
        /// <param name="uiTarget">object the list entry targets, for example a transform, component or component type</param>
        internal class GenericUIListEntry(
            Button selfButton,
            Text entryName,
            Image bgImage,
            GameObject instantiatedUiGo,
            object uiTarget)
        {
            public Button SelfButton = selfButton;
            public Text EntryName = entryName;
            public Image BgImage = bgImage;
            public GameObject UiGO = instantiatedUiGo;
            public object UiTarget = uiTarget;

            public void SetBgColorEdited()  => BgImage.color = ENTRY_BG_COLOR_EDITED;
            public void SetBgColorDefault() => BgImage.color = ENTRY_BG_COLOR_DEFAULT;
        }

        /// <summary>
        /// Container for a property entry.
        /// </summary>
        /// <param name="resetButton">reset button component of the property entry</param>
        /// <param name="propertyName">label component of the property entry</param>
        /// <param name="bgImage">image component of the property entry</param>
        /// <param name="instantiatedUiGo">instantiated ui GameObject</param>
        /// <param name="uiComponentSetValueResetDelegate">this delegate is used by the reset button to reset the ui value, return effective value</param>
        internal class PropertyUIEntry(
            Button timelineButton,
            Button resetButton,
            Text propertyName,
            Image bgImage,
            GameObject instantiatedUiGo,
            Func<object, object> uiComponentSetValueResetDelegate)
        {
            public Button TimelineButton = timelineButton;
            public Button ResetButton = resetButton;
            public Text PropertyName = propertyName;
            /// <summary>
            /// Name of the property / field this entry shows.
            /// </summary>
            public string MemberName;
            public Image BgImage = bgImage;
            public GameObject UiGO = instantiatedUiGo;
            /// <summary>
            /// This delegate is used by the reset button to reset the ui value, return effective value.
            /// </summary>
            public Func<object, object> UiComponentSetValueResetDelegate = uiComponentSetValueResetDelegate;
            /// <summary>
            /// The singular Selectable inside the UI entry, for example an InputField or Toggle.
            /// </summary>
            public Selectable UiSelectable;
            /// <summary>
            /// Behaviour to keep flag enum dropdown list open, added to UiSelectable the first time it encountersa flag enum.
            /// </summary>
            public KeepOpenFlagsDropdown KeepOpen;

            /// <summary>
            /// Currently only used for saving a reference to the referenced object inside ObjectInspector.
            /// </summary>
            public object Wrapper;

            public void SetBgColorEdited()  => BgImage.color = ENTRY_BG_COLOR_EDITED;
            public void SetBgColorDefault() => BgImage.color = ENTRY_BG_COLOR_DEFAULT;

            public void SetUiComponentTargetValue(object value) => UiComponentSetValueResetDelegate?.Invoke(value);
        }
    }
}
