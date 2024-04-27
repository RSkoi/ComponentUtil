using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RSkoi_ComponentUtil.UI
{
    internal class ComponentUtilUI
    {
        private static GameObject _canvas;
        private static Transform _container;

        private static GameObject _genericListEntryPrefab;
        private static GameObject _componentPropertyDecimalEntryPrefab;
        private static GameObject _componentPropertyEnumEntryPrefab;
        private static GameObject _componentPropertyBoolEntryPrefab;

        private static Transform transformListContainer;
        // Dictionary<ListEntryGO, TargetTransform>
        private readonly static Dictionary<GameObject, Transform> _transformListEntries = [];
        private static GameObject _selectedGO;

        private static Text componentListSelectedGOText;
        private static Transform componentListContainer;
        // Dictionary<ListEntryGO, TargetComponent>
        private readonly static Dictionary<GameObject, Component> _componentListEntries = [];
        private static Component _selectedComponent;

        private static Text componentPropertyListSelectedComponentText;
        private static Transform componentPropertyListContainer;
        // Dictionary<ListEntryGO, TargetField>
        private static readonly Dictionary<GameObject, PropertyInfo> _componentPropertyListEntries = [];
        private static readonly Dictionary<GameObject, FieldInfo> _componentFieldListEntries = [];

        public static void Init()
        {
            LoadUIResources();
            InstantiateUI();
        }

        public static void ToggleWindow()
        {
            if (_canvas.activeSelf)
            {
                SetEntriesInactive();
                HideWindow();
            }
            else
            {
                ShowWindow();
                PopulateMenu();
                searchSection.CheckState();
            }
        }

        #region private
        private static void LoadUIResources()
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RSkoi_ComponentUtil.Resources.componentutil.unity3d");
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            _componentUtilUIPrefab = AssetBundle.LoadFromMemory(buffer).LoadAsset<GameObject>("ComponentUtilCanvas");
            _componentUtilPropertyEntryPrefab = AssetBundle.LoadFromMemory(buffer).LoadAsset<GameObject>("ComponentUtilPropertyEntry");
            stream.Close();
        }

        private static void InstantiateUI()
        {
            _canvas = GameObject.Instantiate(_componentUtilUIPrefab);
            _canvas.SetActive(false);
            _container = _canvas.transform.Find("ComponentUtilContainer");

            propertyEntryScrollRect = _container.Find("TrackerList/PropertyEntryScrollView").GetComponent<ScrollRect>();

            searchSection = new(_container.Find("AddComponentContainer/Search").gameObject, false);
            resolveDuplicatesSection = new(searchSection, _container.Find("AddComponentContainer/ResolveDuplicates").gameObject, false);
            resolveComponentSection = new(resolveDuplicatesSection, _container.Find("AddComponentContainer/ResolveComponent").gameObject, false);
            addSection = new(resolveComponentSection, _container.Find("AddComponentContainer/Add").gameObject, false);

            ComponentUtilDraggable draggable = _container.Find("LabelDragPanel").gameObject.AddComponent<ComponentUtilDraggable>();
            draggable.target = _container.GetComponent<RectTransform>();
        }
        #endregion private
    }
}
