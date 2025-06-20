﻿using UnityEngine.Events;
using UnityEngine.SceneManagement;
using BepInEx;
using BepInEx.Logging;
using KKAPI.Studio.SaveLoad;

using RSkoi_ComponentUtil.UI;
using RSkoi_ComponentUtil.Core;
using RSkoi_ComponentUtil.Scene;
using RSkoi_ComponentUtil.Timeline;

namespace RSkoi_ComponentUtil
{
    [BepInProcess("CharaStudio.exe")]
    [BepInDependency(KKAPI.KoikatuAPI.GUID, KKAPI.KoikatuAPI.VersionConst)]
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public partial class ComponentUtil : BaseUnityPlugin
    {
        internal const string PLUGIN_GUID = "RSkoi_ComponentUtil";
        internal const string PLUGIN_NAME = "RSkoi_ComponentUtil";
        internal const string PLUGIN_VERSION = "1.4.2";

        internal static ComponentUtil _instance;
        internal static ManualLogSource _logger;

        private void Awake()
        {
            _instance = this;
            _logger = Logger;

            SetupConfig();
        }

        private void Update()
        {
            UpdateConfig();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += new UnityAction<UnityEngine.SceneManagement.Scene, LoadSceneMode>(LoadedEvent);
            StudioSaveLoadApi.RegisterExtraBehaviour<ComponentUtilSceneBehaviour>(PLUGIN_GUID);
        }

        private void LoadedEvent(UnityEngine.SceneManagement.Scene scene, LoadSceneMode loadMode)
        {
#if KK
            if (scene.buildIndex != 1)
                return;
#elif KKS
            if (scene.buildIndex != 2)
                return;
#endif

            ComponentUtilUI.Init();
            ComponentUtilTimeline.Init();
            ComponentUtilCache.GetOrCacheComponentAdders(true);
        }
    }
}
