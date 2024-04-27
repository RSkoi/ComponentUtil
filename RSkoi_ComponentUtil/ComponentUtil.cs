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
        internal static ConfigEntry<float> UiScale { get; private set; }
        internal static ConfigEntry<KeyboardShortcut> ToggleUI { get; private set; }
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
                0.7f,
                new ConfigDescription("Scales the UI to given factor. Reopen ComponentUtil window for the change to apply.",
                null,
                new ConfigurationManagerAttributes { Order = 1 }));

            ToggleUI = Config.Bind(
                "Keyboard Shortcuts",
                "Toggle UI",
                new KeyboardShortcut(KeyCode.M, KeyCode.RightControl),
                new ConfigDescription("Toggle the UI of ComponentUtil.",
                null,
                new ConfigurationManagerAttributes { Order = 2 }));
        }

        private void LoadedEvent(UnityEngine.SceneManagement.Scene scene, LoadSceneMode loadMode)
        {
            if (scene.buildIndex != 1)
                return;

            ComponentUtilUI.Init();
        }
    }
}
