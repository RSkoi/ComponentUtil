using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RSkoi_ComponentUtil.UI
{
    internal class ComponentUtilUI
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

        #region list entries
        // Dictionary<ListEntryGO, Target>
        internal readonly static Dictionary<GameObject, Transform> _transformListEntries = [];
        internal readonly static Dictionary<GameObject, Component> _componentListEntries = [];
        internal readonly static Dictionary<GameObject, PropertyInfo> _componentPropertyListEntries = [];
        internal readonly static Dictionary<GameObject, FieldInfo> _componentFieldListEntries = [];
        #endregion list entries

        #region selected text
        internal static Text _componentListSelectedGOText;
        internal static Text _componentPropertyListSelectedComponentText;
        #endregion selected text

        // this will be overwritten in InstantiateUI()
        private static float _baseCanvasReferenceResolutionY = 600f;

        public static void Init()
        {
            LoadUIResources();
            InstantiateUI();
        }

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

        private static void ShowWindow()
        {
            _canvasContainer.gameObject.SetActive(true);
            float uiScale = ComponentUtil.UiScale.Value;
            // aiiee float division, what could go wrong
            _canvasScaler.referenceResolution = new(_canvasScaler.referenceResolution.x, _baseCanvasReferenceResolutionY / uiScale);
        }

        private static void HideWindow()
        {
            _canvasContainer.gameObject.SetActive(false);
        }
        #endregion private
    }
}
