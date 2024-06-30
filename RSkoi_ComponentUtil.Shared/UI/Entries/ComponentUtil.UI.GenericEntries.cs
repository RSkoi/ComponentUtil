using System;
using UnityEngine;
using UnityEngine.UI;

namespace RSkoi_ComponentUtil.UI
{
    internal static partial class ComponentUtilUI
    {
        internal static GameObject _genericListEntryPrefab;

        internal static PropertyUIEntry PreConfigureNewUiEntry(GameObject entry, GameObject usedPrefab)
        {
            Button resetButton = entry.transform.Find("ResetButton").GetComponent<Button>();
            Text entryname = entry.transform.Find("EntryLabel").GetComponent<Text>();
            Image bgImage = entry.transform.Find("EntryBg").GetComponent<Image>();
            return new(resetButton, entryname, bgImage, null, entry, usedPrefab, null);
        }

        internal static GenericUIListEntry PreConfigureNewGenericUIListEntry(GameObject entry)
        {
            Button selfButton = entry.GetComponent<Button>();
            Text entryname = entry.transform.Find("EntryLabel").GetComponent<Text>();
            Image bgImage = entry.transform.Find("EntryBg").GetComponent<Image>();
            return new(selfButton, entryname, bgImage, entry, null, null);
        }

        /// <summary>
        /// generic ui list entry consisting of a button, bgImage and label
        /// </summary>
        /// <param name="selfButton">button component of the list entry</param>
        /// <param name="entryName">label component of the list entry</param>
        /// <param name="bgImage">image component of the list entry</param>
        /// <param name="instantiatedUiGo">the instantiated GameObeject (the list entry itself)</param>
        /// <param name="uiTarget">object the list entry targets, for example a transform, component or component type</param>
        /// <param name="parentUiEntry">the parent ui entry, for component entries this would be the transform entry; currently not used</param>
        internal class GenericUIListEntry(
            Button selfButton,
            Text entryName,
            Image bgImage,
            GameObject instantiatedUiGo,
            object uiTarget,
            GenericUIListEntry parentUiEntry)
        {
            //public HashSet<GenericUIListEntry> editedChildren = [];

            public Button SelfButton = selfButton;
            public Text EntryName = entryName;
            public Image BgImage = bgImage;
            public GameObject UiGO = instantiatedUiGo;
            public object UiTarget = uiTarget;
            public GenericUIListEntry ParentUiEntry = parentUiEntry;

            public void SetBgColorEdited(GenericUIListEntry _ /*child*/)
            {
                /*if (child != null)
                    editedChildren.Add(child); // hashset has no duplicates

                if (BgImage.color == ENTRY_BG_COLOR_EDITED)
                    return;*/

                BgImage.color = ENTRY_BG_COLOR_EDITED;
                //ParentUiEntry?.SetBgColorEdited(this);
            }

            public void SetBgColorDefault(GenericUIListEntry _ /*child*/)
            {
                /*if (child != null)
                    editedChildren.Remove(child);

                if (editedChildren.Count > 0)
                    return;*/

                BgImage.color = ENTRY_BG_COLOR_DEFAULT;
                //ParentUiEntry?.SetBgColorDefault(this);
            }

            public void ResetBgAndChildren()
            {
                BgImage.color = ENTRY_BG_COLOR_DEFAULT;
                //editedChildren.Clear();
            }
        }

        /// <summary>
        /// container for a property entry
        /// </summary>
        /// <param name="resetButton">reset button component of the property entry</param>
        /// <param name="propertyName">label component of the property entry</param>
        /// <param name="bgImage">image component of the property entry</param>
        /// <param name="parentUiEntry">the parent ui list entry, here a component entry</param>
        /// <param name="instantiatedUiGo">instantiated ui GameObject</param>
        /// <param name="usedPrefab">used prefab when instantiating</param>
        /// <param name="uiComponentSetValueResetDelegate">this delegate is used by the reset button to reset the ui value, return effective value</param>
        internal class PropertyUIEntry(
            Button resetButton,
            Text propertyName,
            Image bgImage,
            GenericUIListEntry parentUiEntry,
            GameObject instantiatedUiGo,
            GameObject usedPrefab,
            Func<object, object> uiComponentSetValueResetDelegate)
        {
            public Button ResetButton = resetButton;
            public Text PropertyName = propertyName;
            public string PropertyNameValue
            {
                get { return PropertyName.text.Split(' ')[1]; }
            }
            public Image BgImage = bgImage;
            public GameObject UiGO = instantiatedUiGo;
            /// <summary>
            /// the parent ui list entry, here a component entry
            /// </summary>
            public GenericUIListEntry ParentUiEntry = parentUiEntry;
            /// <summary>
            /// could be useful in determining what kind of property entry we are dealing with
            /// </summary>
            public GameObject UsedPrefab = usedPrefab;
            /// <summary>
            /// this delegate is used by the reset button to reset the ui value, return effective value
            /// </summary>
            public Func<object, object> UiComponentSetValueResetDelegate = uiComponentSetValueResetDelegate;
            /// <summary>
            /// if this delegate is not null it will be used by the reset button
            /// </summary>
            public Func<object, object> ResetOverrideDelegate;

            /// <summary>
            /// reference to the wrapper object if one is provided, currently unused
            /// </summary>
            public object Wrapper;

            /* Originally a value change of a property would trigger SetBgColorEdited and call the same on its parent,
             * i.e. GenericUIListEntry, and propagate all the way to a transform ui entry (transform list).
             * This removes the necessity to traverse all visible list entries, but makes the whole thing way too annoying
             * when pages and filter strings come into play. See comment in ComponentUtilUI.TraverseAndSetEditedParents
             */

            /*public PropertyUIEntry(PropertyUIEntry prop, object wrapper)
                : this(prop.ResetButton,
                       prop.PropertyName,
                       prop.BgImage,
                       prop.ParentUiEntry,
                       prop.UiGO,
                       prop.UsedPrefab,
                       prop.UiComponentSetValueDelegateForReset)
            {
                Wrapper = wrapper;
            }*/

            public void SetBgColorEdited()
            {
                BgImage.color = ENTRY_BG_COLOR_EDITED;

                // this must never be the case for property/field entries
                /*if (ParentUiEntry == null)
                {
                    ComponentUtil.logger.LogError($"Property/field PropertyUIEntry with name {PropertyName} has null as ParentUiEntry");
                    return;
                }
                ParentUiEntry.SetBgColorEdited(this);*/
            }

            public void SetBgColorDefault()
            {
                BgImage.color = ENTRY_BG_COLOR_DEFAULT;

                // this must never be the case for property/field entries
                /*if (ParentUiEntry == null)
                {
                    ComponentUtil.logger.LogError($"Property/field PropertyUIEntry with name {PropertyName} has null as ParentUiEntry");
                    return;
                }
                ParentUiEntry.SetBgColorDefault(this);*/
            }

            public void SetUiComponentTargetValue(object value)
            {
                UiComponentSetValueResetDelegate?.Invoke(value);
            }

            public void ResetBgAndChildren()
            {
                BgImage.color = ENTRY_BG_COLOR_DEFAULT;
                //editedChildren.Clear();
            }
        }
    }
}
