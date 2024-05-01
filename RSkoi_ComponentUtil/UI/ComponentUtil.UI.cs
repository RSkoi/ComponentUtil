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
        #region containers
        internal static GameObject _canvasContainer;
        internal static CanvasScaler _canvasScaler;

        internal static Transform _transformWindow;
        internal static CanvasGroup _transformWindowCanvasGroup;
        internal static Transform _componentWindow;
        internal static CanvasGroup _componentWindowCanvasGroup;
        internal static Transform _inspectorWindow;
        #endregion

        #region prefabs
        private static GameObject _canvasPrefab;
        internal static GameObject _genericListEntryPrefab;
        internal static GameObject _componentPropertyDecimalEntryPrefab;
        internal static GameObject _componentPropertyEnumEntryPrefab;
        internal static GameObject _componentPropertyBoolEntryPrefab;
        #endregion prefabs

        #region hide buttons
        private static Button _hideTransformListButton;
        private static Button _hideComponentListButton;
        #endregion hide buttons

        #region scroll view content containers
        internal static Transform _transformListContainer;
        internal static Transform _componentListContainer;
        internal static Transform _componentPropertyListContainer;
        #endregion scroll view content containers

        #region entry bg colors
        private static readonly Color ENTRY_BG_COLOR_DEFAULT = Color.white;
        private static readonly Color ENTRY_BG_COLOR_EDITED = Color.green;
        #endregion entry bg colors

        #region selected text
        internal static Text _componentListSelectedGOText;
        internal static Text _componentPropertyListSelectedComponentText;
        #endregion selected text

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
            if (_canvasContainer.activeSelf)
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
            _canvasContainer.gameObject.SetActive(true);
            float uiScale = ComponentUtil.UiScale.Value;
            // aiiee float division, what could go wrong
            _canvasScaler.referenceResolution = new(_canvasScaler.referenceResolution.x, _baseCanvasReferenceResolutionY / uiScale);
        }

        /// <summary>
        /// hides the ui
        /// </summary>
        public static void HideWindow()
        {
            _canvasContainer.gameObject.SetActive(false);
        }

        #region internal
        internal static void TraverseAndSetEditedParents()
        {
            foreach (var key in ComponentUtil._tracker.Keys)
            {
                Transform t = key.ObjCtrlInfo.guideObject.transformTarget;
                Component c = key.Component;

                // TODO: very slow iteration until entries are found, improve with hashing of UiTarget/t/c?

                foreach (var entry in TransformListEntries)
                {
                    if (entry.UiGO.activeSelf
                        && entry.BgImage?.color != ENTRY_BG_COLOR_EDITED
                        && (Transform)entry.UiTarget == t)
                    {
                        entry.SetBgColorEdited(null);
                        break;
                    }
                }

                foreach (var entry in ComponentListEntries)
                {
                    if (entry.UiGO.activeSelf
                        && entry.BgImage?.color != ENTRY_BG_COLOR_EDITED
                        && (Component)entry.UiTarget == c)
                    {
                        entry.SetBgColorEdited(null);
                        break;
                    }
                }
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
            _canvasContainer.SetActive(false);
            // window containers
            _canvasScaler = _canvasContainer.GetComponent<CanvasScaler>();
            _transformWindow = _canvasContainer.transform.Find("TransformListContainer");
            _componentWindow = _canvasContainer.transform.Find("ComponentListContainer");
            _inspectorWindow = _canvasContainer.transform.Find("ComponentInspectorContainer");
            _transformWindowCanvasGroup = _transformWindow.GetComponent<CanvasGroup>();
            _componentWindowCanvasGroup = _componentWindow.GetComponent<CanvasGroup>();

            // scroll view content containers
            _transformListContainer = _transformWindow.Find("TransformList/TransformEntryScrollView/Viewport/Content");
            _componentListContainer = _componentWindow.Find("ComponentList/ComponentEntryScrollView/Viewport/Content");
            _componentPropertyListContainer = _inspectorWindow.Find("ComponentPropertyList/ComponentPropertyEntryScrollView/Viewport/Content");

            // window tooltips
            _componentListSelectedGOText = _componentWindow.Find("ComponentList/ComponentText").GetComponent<Text>();
            _componentPropertyListSelectedComponentText = _inspectorWindow.Find("ComponentPropertyList/ComponentText").GetComponent<Text>();

            // buttons to hide windows
            _hideTransformListButton = _componentWindow.Find("ToggleTransformListButton").GetComponent<Button>();
            _hideTransformListButton.onClick.AddListener(() => ToggleCanvasGroup(_transformWindowCanvasGroup));
            _hideComponentListButton = _inspectorWindow.Find("ToggleComponentListButton").GetComponent<Button>();
            _hideComponentListButton.onClick.AddListener(() => ToggleCanvasGroup(_componentWindowCanvasGroup));

            // draggables
            SetupDraggable(_transformWindow);
            SetupDraggable(_componentWindow);
            SetupDraggable(_inspectorWindow);

            _baseCanvasReferenceResolutionY = _canvasScaler.referenceResolution.y;
        }

        private static void SetupDraggable(Transform windowContainer)
        {
            ComponentUtilDraggable draggable = windowContainer.Find("LabelDragPanel").gameObject.AddComponent<ComponentUtilDraggable>();
            draggable.target = windowContainer.GetComponent<RectTransform>();
        }

        private static void ToggleCanvasGroup(CanvasGroup group)
        {
            if (group.blocksRaycasts)
                HideCanvasGroup(group);
            else
                ShowCanvasGroup(group);
        }

        private static void HideCanvasGroup(CanvasGroup group)
        {
            // blocksRaycasts=false prevents interactions
            group.blocksRaycasts = false;
            group.alpha = 0.0f;
        }

        private static void ShowCanvasGroup(CanvasGroup group)
        {
            group.blocksRaycasts = true;
            group.alpha = 1.0f;
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
            public HashSet<GenericUIListEntry> editedChildren = [];

            public Button SelfButton = selfButton;
            public Text EntryName = entryName;
            public Image BgImage = bgImage;
            public GameObject UiGO = instantiatedUiGo;
            public object UiTarget = uiTarget;
            public GenericUIListEntry ParentUiEntry = parentUiEntry;

            public void SetBgColorEdited(GenericUIListEntry child)
            {
                if (child != null)
                    editedChildren.Add(child); // hashset has no duplicates

                if (BgImage.color == ENTRY_BG_COLOR_EDITED)
                    return;

                BgImage.color = ENTRY_BG_COLOR_EDITED;
                ParentUiEntry?.SetBgColorEdited(this);
            }

            public void SetBgColorDefault(GenericUIListEntry child)
            {
                if (child != null)
                    editedChildren.Remove(child);

                if (editedChildren.Count > 0)
                    return;

                BgImage.color = ENTRY_BG_COLOR_DEFAULT;
                ParentUiEntry?.SetBgColorDefault(this);
            }

            public void ResetBgAndChildren()
            {
                BgImage.color = ENTRY_BG_COLOR_DEFAULT;
                editedChildren.Clear();
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
            : GenericUIListEntry(null, propertyName, bgImage, instantiatedUiGo, null, parentUiEntry)
        {
            public Button ResetButton = resetButton;
            public Text PropertyName = propertyName;
            public new Image BgImage = bgImage;
            public new GameObject UiGO = instantiatedUiGo;
            // the parent ui list entry, here a component entry
            public new GenericUIListEntry ParentUiEntry = parentUiEntry;
            // could be useful in determining what kind of property entry we are dealing with
            public GameObject UsedPrefab = usedPrefab;
            // this delegate is used by the reset button
            public Func<object, object> UiComponentSetValueDelegateForReset = uiComponentSetValueDelegateForReset;

            public void SetBgColorEdited()
            {
                BgImage.color = ENTRY_BG_COLOR_EDITED;

                // this must never be the case for property/field entries
                if (ParentUiEntry == null)
                {
                    ComponentUtil.logger.LogError($"Property/field PropertyUIEntry with name {PropertyName} has null as ParentUiEntry");
                    return;
                }
                ParentUiEntry.SetBgColorEdited(this);
            }

            public void SetBgColorDefault()
            {
                BgImage.color = ENTRY_BG_COLOR_DEFAULT;

                // this must never be the case for property/field entries
                if (ParentUiEntry == null)
                {
                    ComponentUtil.logger.LogError($"Property/field PropertyUIEntry with name {PropertyName} has null as ParentUiEntry");
                    return;
                }
                ParentUiEntry.SetBgColorDefault(this);
            }

            public void SetUiComponentTargetValue(object value)
            {
                UiComponentSetValueDelegateForReset?.Invoke(value);
            }
        }
        #endregion internal UI container classes
    }
}
