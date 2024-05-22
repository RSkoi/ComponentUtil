using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RSkoi_ComponentUtil.UI
{
    internal static partial class ComponentUtilUI
    {
        #region prefabs
        private static GameObject _canvasPrefab;
        internal static GameObject _genericListEntryPrefab;
        internal static GameObject _componentPropertyDecimalEntryPrefab;
        internal static GameObject _componentPropertyEnumEntryPrefab;
        internal static GameObject _componentPropertyBoolEntryPrefab;
        #endregion prefabs

        #region canvas and containers
        internal static GameObject _canvasContainer;
        internal static Canvas _canvas;
        internal static CanvasScaler _canvasScaler;
        /// <summary>
        /// use this to determine whether the UI is visible
        /// </summary>
        public static bool CanvasIsActive
        {
            get
            {
                if (_canvas != null)
                    return _canvas.enabled;
                return false;
            }
        }

        internal static Transform _transformWindow;
        internal static Transform _componentWindow;
        internal static Transform _inspectorWindow;

        internal static Transform _componentAdderWindow;
        #endregion canvas and containers

        #region scroll view content containers
        internal static Transform _transformListContainer;
        internal static Transform _componentListContainer;
        internal static Transform _componentPropertyListContainer;

        internal static Transform _componentAdderListContainer;
        #endregion scroll view content containers

        #region hide buttons
        private static Button _hideTransformListButton;
        private static Button _hideComponentListButton;
        private static Button _toggleComponentAdderButton;
        #endregion hide buttons

        #region entry bg colors
        private static readonly Color ENTRY_BG_COLOR_DEFAULT = Color.white;
        private static readonly Color ENTRY_BG_COLOR_EDITED = Color.green;
        #endregion entry bg colors

        #region selected text
        internal static Text _componentListSelectedGOText;
        internal static Text _componentPropertyListSelectedComponentText;

        internal static Text _componentAdderListSelectedGOText;
        #endregion selected text

        #region pages
        #region transform list
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
        #endregion transform list

        #region component list
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
        #endregion component list

        #region component adder list
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
        #endregion component adder list
        #endregion pages

        #region inspector buttons
        internal static Button _componentDeleteButton;
        #endregion inspector buttons

        // this will be overwritten in InstantiateUI()
        private static float _baseCanvasReferenceResolutionY = 600f;

        /// <summary>
        /// initializes the UI, call only once
        /// </summary>
        public static void Init()
        {
            LoadUIResources();
            InstantiateUI();
        }

        /// <summary>
        /// toggles the UI, uses first selected object in workspace as input for entry point
        /// </summary>
        public static void ToggleWindow()
        {
            if (CanvasIsActive)
                HideWindow();
            else
            {
                ShowWindow();
                var selected = KKAPI.Studio.StudioAPI.GetSelectedObjects();
                if (selected.Any())
                    ComponentUtil._instance.Entry(selected.First());
            }
        }

        /// <summary>
        /// shows the ui, sets referenceResolution of CanvasScaler
        /// </summary>
        public static void ShowWindow()
        {
            _canvas.enabled = true;
            float uiScale = ComponentUtil.UiScale.Value;
            _canvasScaler.referenceResolution = new(
                _canvasScaler.referenceResolution.x,
                _baseCanvasReferenceResolutionY / uiScale); // aiiee float division, what could go wrong
        }

        /// <summary>
        /// hides the ui
        /// </summary>
        public static void HideWindow()
        {
            _canvas.enabled = false;
        }

        #region internal
        internal static void TraverseAndSetEditedParents()
        {
            /* Run every time the state of the ui list entry 'edited' state could have changed
             * It's not pretty, but it works
             * 
             * Maximum amount of entries to iterate through is ComponentUtil.ItemsPerPageValue (bepin config) * 2
             * n = ComponentUtil.ItemsPerPageValue, m = ComponentUtil._tracker.Keys.Count
             * 2*O(m) + 2*O(n) == O(m) + O(n), right? could be worse...
             * 
             * => realistically, there are always more entries in the pool than in the tracker => O(n) wins
             * => the more entries in the tracker and the bigger the pool, the worse this gets
             */

            HashSet<Transform> trackedTransforms = ComponentUtil.TrackedTransforms;
            HashSet<Component> trackedComponents = ComponentUtil.TrackedComponents;

            foreach (var entry in TransformListEntries)
            {
                if (!entry.UiGO.activeSelf)
                    continue;

                bool defaultColor = entry.BgImage.color != ENTRY_BG_COLOR_EDITED;
                bool tracked = trackedTransforms.Contains((Transform)entry.UiTarget);
                if (defaultColor && tracked)
                    entry.SetBgColorEdited(null);
                else if (!defaultColor && !tracked)
                    entry.SetBgColorDefault(null);
            }

            foreach (var entry in ComponentListEntries)
            {
                if (!entry.UiGO.activeSelf)
                    continue;

                bool defaultColor = entry.BgImage.color != ENTRY_BG_COLOR_EDITED;
                bool tracked = trackedComponents.Contains((Component)entry.UiTarget);
                if (defaultColor && tracked)
                    entry.SetBgColorEdited(null);
                else if (!defaultColor && !tracked)
                    entry.SetBgColorDefault(null);
            }
        }

        internal static GameObject MapPropertyOrFieldToEntryPrefab(Type t)
        {
            if (t.IsEnum)
                return _componentPropertyEnumEntryPrefab;
            else if (t.Equals(typeof(bool)))
                return _componentPropertyBoolEntryPrefab;

            return _componentPropertyDecimalEntryPrefab;
        }

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

        internal static void UpdateUISelectedText(Text uiText, string selectedName, char separator = ':')
        {
            int splitIndex = uiText.text.IndexOf(separator);
            string newText = uiText.text.Substring(0, splitIndex + 1);
            newText += " <b>" + selectedName + "</b>";
            uiText.text = newText;
        }

        internal static void ResetPageNumbers()
        {
            ResetPageNumberTransform();
            ResetPageNumberComponent();
        }

        internal static void UpdatePageNumberTransform(int pageNumber)
        {
            _currentPageTransformText.text = (pageNumber + 1).ToString();
        }

        internal static void ResetPageNumberTransform()
        {
            _currentPageTransformText.text = "1";
        }

        internal static void UpdatePageNumberComponent(int pageNumber)
        {
            _currentPageComponentText.text = (pageNumber + 1).ToString();
        }

        internal static void ResetPageNumberComponent()
        {
            _currentPageComponentText.text = "1";
        }

        internal static void UpdatePageNumberComponentAdder(int pageNumber)
        {
            _currentPageComponentAdderText.text = (pageNumber + 1).ToString();
        }

        internal static void ResetPageNumberComponentAdder()
        {
            _currentPageComponentAdderText.text = "1";
        }
        #endregion internal

        #region private
        private static void LoadUIResources()
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RSkoi_ComponentUtil.Resources.componentutil.unity3d");
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);

            _canvasPrefab = AssetBundle.LoadFromMemory(buffer).LoadAsset<GameObject>("ComponentUtilCanvas");
            _genericListEntryPrefab = AssetBundle.LoadFromMemory(buffer).LoadAsset<GameObject>("ListEntry");
            _componentPropertyDecimalEntryPrefab = AssetBundle.LoadFromMemory(buffer).LoadAsset<GameObject>("ComponentPropertyEntry_Decimal");
            _componentPropertyEnumEntryPrefab = AssetBundle.LoadFromMemory(buffer).LoadAsset<GameObject>("ComponentPropertyEntry_Enum");
            _componentPropertyBoolEntryPrefab = AssetBundle.LoadFromMemory(buffer).LoadAsset<GameObject>("ComponentPropertyEntry_Bool");

            stream.Close();
        }

        private static void InstantiateUI()
        {
            _canvasContainer = GameObject.Instantiate(_canvasPrefab);
            _canvas = _canvasContainer.GetComponent<Canvas>();
            _canvas.enabled = false;
            // window containers
            _canvasScaler = _canvasContainer.GetComponent<CanvasScaler>();
            _transformWindow = _canvasContainer.transform.Find("TransformListContainer");
            _componentWindow = _canvasContainer.transform.Find("ComponentListContainer");
            _inspectorWindow = _canvasContainer.transform.Find("ComponentInspectorContainer");
            _componentAdderWindow = _canvasContainer.transform.Find("ComponentAdderContainer");

            // scroll view content containers
            _transformListContainer = _transformWindow.Find("TransformList/TransformEntryScrollView/Viewport/Content");
            _componentListContainer = _componentWindow.Find("ComponentList/ComponentEntryScrollView/Viewport/Content");
            _componentPropertyListContainer = _inspectorWindow.Find("ComponentPropertyList/ComponentPropertyEntryScrollView/Viewport/Content");
            _componentAdderListContainer = _componentAdderWindow.Find("ComponentAddList/ComponentAddEntryScrollView/Viewport/Content");

            // window tooltips
            _componentListSelectedGOText = _componentWindow.Find("ComponentList/ComponentText").GetComponent<Text>();
            _componentPropertyListSelectedComponentText = _inspectorWindow.Find("ComponentPropertyList/ComponentText").GetComponent<Text>();
            _componentAdderListSelectedGOText = _componentAdderWindow.Find("ComponentAddList/ComponentAddListText").GetComponent<Text>();

            _componentDeleteButton = _inspectorWindow.Find("ComponentPropertyList/DeleteComponentButton").GetComponent<Button>();

            // buttons to hide windows
            _hideTransformListButton = _componentWindow.Find("ToggleTransformListButton").GetComponent<Button>();
            _hideTransformListButton.onClick.AddListener(() => ToggleSubWindow(_transformWindow));
            _hideComponentListButton = _inspectorWindow.Find("ToggleComponentListButton").GetComponent<Button>();
            _hideComponentListButton.onClick.AddListener(() => ToggleSubWindow(_componentWindow));

            _toggleComponentAdderButton = _componentWindow.Find("ComponentList/ToggleComponentAdderButton").GetComponent<Button>();
            _toggleComponentAdderButton.onClick.AddListener(() => ToggleSubWindow(_componentAdderWindow));

            // page buttons
            Transform page = _transformWindow.Find("TransformList/PageContainer");
            _pageSearchTransformInput = page.Find("SearchInput").GetComponent<InputField>();
            _pageSearchTransformInput.onValueChanged.AddListener((s) => ComponentUtil._instance.OnFilterTransform());
            _currentPageTransformText = page.Find("PageCurrentLabel").GetComponent<Text>();
            ResetPageNumberTransform();
            _pageLastTransformButton = page.Find("PageLast").GetComponent<Button>();
            _pageLastTransformButton.onClick.AddListener(ComponentUtil._instance.OnLastTransformPage);
            _pageNextTransformButton = page.Find("PageNext").GetComponent<Button>();
            _pageNextTransformButton.onClick.AddListener(ComponentUtil._instance.OnNextTransformPage);

            page = _componentWindow.Find("ComponentList/PageContainer");
            _pageSearchComponentInput = page.Find("SearchInput").GetComponent<InputField>();
            _pageSearchComponentInput.onValueChanged.AddListener((s) => ComponentUtil._instance.OnFilterComponent());
            _currentPageComponentText = page.Find("PageCurrentLabel").GetComponent<Text>();
            ResetPageNumberComponent();
            _pageLastComponentButton = page.Find("PageLast").GetComponent<Button>();
            _pageLastComponentButton.onClick.AddListener(ComponentUtil._instance.OnLastComponentPage);
            _pageNextComponentButton = page.Find("PageNext").GetComponent<Button>();
            _pageNextComponentButton.onClick.AddListener(ComponentUtil._instance.OnNextComponentPage);

            page = _componentAdderWindow.Find("ComponentAddList/PageContainer");
            _pageSearchComponentAdderInput = page.Find("SearchInput").GetComponent<InputField>();
            _pageSearchComponentAdderInput.onValueChanged.AddListener((s) => ComponentUtil._instance.OnFilterComponentAdder());
            _currentPageComponentAdderText = page.Find("PageCurrentLabel").GetComponent<Text>();
            ResetPageNumberComponentAdder();
            _pageLastComponentAdderButton = page.Find("PageLast").GetComponent<Button>();
            _pageLastComponentAdderButton.onClick.AddListener(ComponentUtil._instance.OnLastComponentAdderPage);
            _pageNextComponentAdderButton = page.Find("PageNext").GetComponent<Button>();
            _pageNextComponentAdderButton.onClick.AddListener(ComponentUtil._instance.OnNextComponentAdderPage);

            // prepare pools
            int itemsPerPage = ComponentUtil.ItemsPerPageValue;
            PrepareTransformPool(itemsPerPage);
            PrepareComponentPool(itemsPerPage);
            PrepareComponentAdderPool(itemsPerPage);

            // draggables
            SetupDraggable(_transformWindow);
            SetupDraggable(_componentWindow);
            SetupDraggable(_inspectorWindow);
            SetupDraggable(_componentAdderWindow);

            _baseCanvasReferenceResolutionY = _canvasScaler.referenceResolution.y;
        }

        private static void SetupDraggable(Transform windowContainer)
        {
            ComponentUtilDraggable draggable = windowContainer.Find("LabelDragPanel").gameObject.AddComponent<ComponentUtilDraggable>();
            draggable.target = windowContainer.GetComponent<RectTransform>();
        }

        private static void ToggleSubWindow(Transform container)
        {
            if (container.gameObject.activeSelf)
                container.gameObject.SetActive(false);
            else
                container.gameObject.SetActive(true);
        }
        #endregion private

        #region internal UI container classes
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

        internal class PropertyUIEntry(
            Button resetButton,
            Text propertyName,
            Image bgImage,
            GenericUIListEntry parentUiEntry,
            GameObject instantiatedUiGo,
            GameObject usedPrefab,
            Func<object, object> uiComponentSetValueDelegateForReset)
        {
            public Button ResetButton = resetButton;
            public Text PropertyName = propertyName;
            public Image BgImage = bgImage;
            public GameObject UiGO = instantiatedUiGo;
            // the parent ui list entry, here a component entry
            public GenericUIListEntry ParentUiEntry = parentUiEntry;
            // could be useful in determining what kind of property entry we are dealing with
            public GameObject UsedPrefab = usedPrefab;
            // this delegate is used by the reset button
            public Func<object, object> UiComponentSetValueDelegateForReset = uiComponentSetValueDelegateForReset;

            /* Originally a value change of a property would trigger SetBgColorEdited and call the same on its parent,
             * i.e. GenericUIListEntry, and propagate all the way to a transform ui entry (transform list).
             * This removes the necessity to traverse all visible list entries, but makes the whole thing way too annoying
             * when pages and filter strings come into play. See comment in ComponentUtilUI.TraverseAndSetEditedParents
             */

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
                UiComponentSetValueDelegateForReset?.Invoke(value);
            }

            public void ResetBgAndChildren()
            {
                BgImage.color = ENTRY_BG_COLOR_DEFAULT;
                //editedChildren.Clear();
            }
        }
        #endregion internal UI container classes
    }
}
