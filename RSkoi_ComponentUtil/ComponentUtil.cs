using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using KKAPI.Utilities;
using KKAPI.Studio.SaveLoad;

using RSkoi_ComponentUtil.UI;
using RSkoi_ComponentUtil.Scene;

namespace RSkoi_ComponentUtil
{
    [BepInProcess("CharaStudio.exe")]
    [BepInDependency(KKAPI.KoikatuAPI.GUID, KKAPI.KoikatuAPI.VersionConst)]
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public partial class ComponentUtil : BaseUnityPlugin
    {
        internal const string PLUGIN_GUID = "RSkoi_ComponentUtil";
        internal const string PLUGIN_NAME = "RSkoi_ComponentUtil";
        internal const string PLUGIN_VERSION = "1.0.0";

        internal static ComponentUtil _instance;

        #region bepinex config
        private const bool DEFAULT_SAVE_SCENE_DATA = true;
        internal static ConfigEntry<bool> SaveSceneData { get; private set; }
        private const bool DEFAULT_LOAD_SCENE_DATA = true;
        internal static ConfigEntry<bool> LoadSceneData { get; private set; }

        private const float DEFAULT_UI_SCALE = 0.7f;
        internal static ConfigEntry<float> UiScale { get; private set; }

        private const KeyCode DEFAULT_TOGGLE_MAIN_KEY = KeyCode.M;
        private const KeyCode DEFAULT_TOGGLE_MODIFIER = KeyCode.RightControl;
        internal static ConfigEntry<KeyboardShortcut> ToggleUI { get; private set; }

        #region ItemsPerPage
        private const int DEFAULT_ITEMS_PER_PAGE = 9;
        internal static ConfigEntry<int> ItemsPerPage { get; private set; }
        internal static int ItemsPerPageValue
        {
            get
            {
                int itemsPerPage = ItemsPerPage.Value;
                if (itemsPerPage <= 0)
                {
                    ItemsPerPage.Value = DEFAULT_ITEMS_PER_PAGE;
                    return DEFAULT_ITEMS_PER_PAGE;
                }
                return itemsPerPage;
            }
        }
        #endregion ItemsPerPage

        #region WaitTimeLoadScene
        private const float DEFAULT_WAIT_TIME_AFTER_LOADING_SCENE_SECONDS = 2f;
        internal static ConfigEntry<float> WaitTimeLoadScene { get; private set; }
        internal static float WaitTimeLoadSceneValue
        {
            get
            {
                float waitTime = WaitTimeLoadScene.Value;
                if (waitTime <= 0)
                {
                    WaitTimeLoadScene.Value = DEFAULT_WAIT_TIME_AFTER_LOADING_SCENE_SECONDS;
                    return DEFAULT_WAIT_TIME_AFTER_LOADING_SCENE_SECONDS;
                }
                return waitTime;
            }
        }
        #endregion WaitTimeLoadScene
        #endregion bepinex config

        internal static ManualLogSource logger;

        private void Awake()
        {
            _instance = this;

            logger = Logger;

            SetupConfig();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += new UnityAction<UnityEngine.SceneManagement.Scene, LoadSceneMode>(LoadedEvent);
            StudioSaveLoadApi.RegisterExtraBehaviour<ComponentUtilSceneBehaviour>(PLUGIN_GUID);
        }

        private void Update()
        {
            if (ToggleUI.Value.IsDown())
                ComponentUtilUI.ToggleWindow();
        }

        private void SetupConfig()
        {
            UiScale = Config.Bind(
                "Config",
                "UI scale",
                DEFAULT_UI_SCALE,
                new ConfigDescription("Scales the UI to given factor. Reopen ComponentUtil window for the change to apply.",
                null,
                new ConfigurationManagerAttributes { Order = 2 }));

            ToggleUI = Config.Bind(
                "Keyboard Shortcuts",
                "Toggle UI",
                new KeyboardShortcut(DEFAULT_TOGGLE_MAIN_KEY, DEFAULT_TOGGLE_MODIFIER),
                new ConfigDescription("Toggle the UI of ComponentUtil.",
                null,
                new ConfigurationManagerAttributes { Order = 1 }));

            ItemsPerPage = Config.Bind(
                "Config",
                "Items per page",
                DEFAULT_ITEMS_PER_PAGE,
                new ConfigDescription("How many items to display in the transform / component list per page. Don't set this too high.",
                null,
                new ConfigurationManagerAttributes { Order = 0 }));

            WaitTimeLoadScene = Config.Bind(
                "Config",
                "Wait time after scene load",
                DEFAULT_WAIT_TIME_AFTER_LOADING_SCENE_SECONDS,
                new ConfigDescription("How long ComponentUtil should wait in seconds after a scene is loaded before applying tracked changes." +
                " Try setting this higher if after loading a scene the changes saved with ComponentUtil seem to be overwritten.",
                null,
                new ConfigurationManagerAttributes { Order = 1 }));

            LoadSceneData = Config.Bind(
                "Config",
                "Apply data on scene load",
                DEFAULT_LOAD_SCENE_DATA,
                new ConfigDescription("Whether ComponentUtil should apply related saved data on scene load." +
                " Set to false for debugging purposes or if a scene fails to load due to ComponentUtil.",
                null,
                new ConfigurationManagerAttributes { Order = 3 }));

            SaveSceneData = Config.Bind(
                "Config",
                "Save data on scene save",
                DEFAULT_SAVE_SCENE_DATA,
                new ConfigDescription("Whether ComponentUtil should save related data on scene save." +
                " Set to false for debugging purposes or if you want to save a 'clean' scene with no edits.",
                null,
                new ConfigurationManagerAttributes { Order = 3 }));
        }

        private void LoadedEvent(UnityEngine.SceneManagement.Scene scene, LoadSceneMode loadMode)
        {
            if (scene.buildIndex != 1)
                return;

            ComponentUtilUI.Init();
        }
    }
}
