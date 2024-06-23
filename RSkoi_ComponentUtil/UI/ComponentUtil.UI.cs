using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Studio;

namespace RSkoi_ComponentUtil.UI
{
    /// <summary>
    /// class is split into multiple files:<br />
    /// - generic stuff (this file)<br />
    /// - ui windows<br />
    /// - ui entries (for ComponentInspector)
    /// </summary>
    internal static partial class ComponentUtilUI
    {
        // overarching canvas prefab
        private static GameObject _canvasPrefab;

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
        #endregion canvas and containers

        #region entry bg colors
        private static readonly Color ENTRY_BG_COLOR_DEFAULT = Color.white;
        private static readonly Color ENTRY_BG_COLOR_EDITED = Color.green;
        #endregion entry bg colors

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
                if (CanOpenWindowOnSelectedObject(selected))
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

            SetRectTransformSizeDelta(_transformWindowRect, _transformWindowRectOriginalSize, ComponentUtil.TransformWindowScaleValue);
            SetRectTransformSizeDelta(_componentWindowRect, _componentWindowRectOriginalSize, ComponentUtil.ComponentWindowScaleValue);
            SetRectTransformSizeDelta(_componentAdderWindowRect, _componentAdderWindowRectOriginalSize, ComponentUtil.ComponentAdderWindowScaleValue);
            SetRectTransformSizeDelta(_inspectorWindowRect, _inspectorWindowRectOriginalSize, ComponentUtil.ComponentInspectorScaleValue);
        }

        /// <summary>
        /// hides the ui
        /// </summary>
        public static void HideWindow()
        {
            _canvas.enabled = false;
        }

        /// <summary>
        /// checks whether ComponentUtil UI window can be opened
        /// </summary>
        /// <param name="objectCtrlInfo">currently selected objects within the workspace</param>
        /// <returns>true if window can be opened, else false</returns>
        public static bool CanOpenWindowOnSelectedObject(IEnumerable<ObjectCtrlInfo> objectCtrlInfo)
        {
            List<ObjectCtrlInfo> selectedObjects = objectCtrlInfo.ToList();

            // force singular selection
            if (selectedObjects.Count != 1)
                return false;

            // folders are not a valid selection
            if (selectedObjects[0].kind == 3)
                return false;

            return true;
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
            else if (t.Equals(typeof(Vector2)) ||
                     t.Equals(typeof(Vector3)) ||
                     t.Equals(typeof(Vector4)) ||
                     t.Equals(typeof(Quaternion)))
                return _componentPropertyVector4EntryPrefab;

            return _componentPropertyDecimalEntryPrefab;
        }

        internal static void UpdateUISelectedText(Text uiText, string selectedName, char separator = ':')
        {
            int splitIndex = uiText.text.IndexOf(separator);
            string newText = uiText.text.Substring(0, splitIndex + 1);
            newText += $" <b>{selectedName}</b>";
            uiText.text = newText;
        }

        internal static void ResetPageNumbers()
        {
            ResetPageNumberTransform();
            ResetPageNumberComponent();
        }

        internal static void SetRectTransformSizeDelta(RectTransform rect, Vector2 originalSize, Vector2 mulSize)
        {
            rect.sizeDelta = new(originalSize.x * mulSize.x, originalSize.y * mulSize.y);
        }
        #endregion internal

        #region private - loading and instantiating
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
            _componentPropertyVector4EntryPrefab = AssetBundle.LoadFromMemory(buffer).LoadAsset<GameObject>("ComponentPropertyEntry_Vector4");

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
            _transformWindowRect = _transformWindow.GetComponent<RectTransform>();
            _componentWindowRect = _componentWindow.GetComponent<RectTransform>();
            _inspectorWindowRect = _inspectorWindow.GetComponent<RectTransform>();
            _componentAdderWindowRect = _componentAdderWindow.GetComponent<RectTransform>();
            _transformWindowRectOriginalSize = _transformWindowRect.sizeDelta;
            _componentWindowRectOriginalSize = _componentWindowRect.sizeDelta;
            _inspectorWindowRectOriginalSize = _inspectorWindowRect.sizeDelta;
            _componentAdderWindowRectOriginalSize = _componentAdderWindowRect.sizeDelta;

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
        #endregion private - loading and instantiating
    }
}
