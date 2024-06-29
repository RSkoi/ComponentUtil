using System;
using UnityEngine;
using BepInEx.Configuration;
using KKAPI.Utilities;

using RSkoi_ComponentUtil.UI;

namespace RSkoi_ComponentUtil
{
    public partial class ComponentUtil
    {
        #region bepinex config
        #region Config
        private const bool DEFAULT_SAVE_SCENE_DATA = true;
        internal static ConfigEntry<bool> SaveSceneData { get; private set; }
        private const bool DEFAULT_LOAD_SCENE_DATA = true;
        internal static ConfigEntry<bool> LoadSceneData { get; private set; }

        #region WaitTimeLoadScene
        private const float DEFAULT_WAIT_TIME_AFTER_LOADING_SCENE_SECONDS = 2f;
        internal static ConfigEntry<float> WaitTimeLoadScene { get; private set; }
        internal static float WaitTimeLoadSceneValue
        {
            get
            {
                return ValidateConfigValue(
                    WaitTimeLoadScene,
                    val => val > 0,
                    DEFAULT_WAIT_TIME_AFTER_LOADING_SCENE_SECONDS);
            }
        }
        #endregion WaitTimeLoadScene
        #endregion Config

        #region Keyboard Shortcuts
        private const KeyCode DEFAULT_TOGGLE_MAIN_KEY = KeyCode.M;
        private const KeyCode DEFAULT_TOGGLE_MODIFIER = KeyCode.RightControl;
        internal static ConfigEntry<KeyboardShortcut> ToggleUI { get; private set; }
        #endregion Keyboard Shortcuts

        #region Config - UI
        private const float DEFAULT_UI_SCALE = 0.7f;
        internal static ConfigEntry<float> UiScale { get; private set; }

        #region WindowScale
        private static readonly Vector2 DEFAULT_WINDOW_SIZE_FACTOR = Vector2.one;
        internal static ConfigEntry<Vector2> TransformWindowScale { get; private set; }
        internal static ConfigEntry<Vector2> ComponentWindowScale { get; private set; }
        internal static ConfigEntry<Vector2> ComponentAdderWindowScale { get; private set; }
        internal static ConfigEntry<Vector2> ComponentInspectorScale { get; private set; }
        internal static ConfigEntry<Vector2> ObjectInspectorScale { get; private set; }

        internal static Vector2 TransformWindowScaleValue
        {
            get
            {
                return ValidateConfigValue(
                    TransformWindowScale,
                    val => (val.x >= 1 && val.y >= 1),
                    DEFAULT_WINDOW_SIZE_FACTOR);
            }
        }
        internal static Vector2 ComponentWindowScaleValue
        {
            get
            {
                return ValidateConfigValue(
                    ComponentWindowScale,
                    val => (val.x >= 1 && val.y >= 1),
                    DEFAULT_WINDOW_SIZE_FACTOR);
            }
        }
        internal static Vector2 ComponentAdderWindowScaleValue
        {
            get
            {
                return ValidateConfigValue(
                    ComponentAdderWindowScale,
                    val => (val.x >= 1 && val.y >= 1),
                    DEFAULT_WINDOW_SIZE_FACTOR);
            }
        }
        internal static Vector2 ComponentInspectorScaleValue
        {
            get
            {
                return ValidateConfigValue(
                    ComponentInspectorScale,
                    val => (val.x >= 1 && val.y >= 1),
                    DEFAULT_WINDOW_SIZE_FACTOR);
            }
        }
        internal static Vector2 ObjectInspectorScaleValue
        {
            get
            {
                return ValidateConfigValue(
                    ObjectInspectorScale,
                    val => (val.x >= 1 && val.y >= 1),
                    DEFAULT_WINDOW_SIZE_FACTOR);
            }
        }
        #endregion WindowScale

        #region ItemsPerPage
        private const int DEFAULT_ITEMS_PER_PAGE = 9;
        internal static ConfigEntry<int> ItemsPerPage { get; private set; }
        internal static int ItemsPerPageValue
        {
            get
            {
                return ValidateConfigValue(
                    ItemsPerPage,
                    val => val > 0,
                    DEFAULT_ITEMS_PER_PAGE);
            }
        }
        #endregion ItemsPerPage
        #endregion Config - UI
        #endregion bepinex config

        private static T ValidateConfigValue<T>(ConfigEntry<T> config, Func<T, bool> validateFunc, T defaultValue)
        {
            T val = config.Value;
            if (!validateFunc(val))
            {
                config.Value = defaultValue;
                return defaultValue;
            }
            return val;
        }

        private void UpdateConfig()
        {
            if (ToggleUI.Value.IsDown())
                ComponentUtilUI.ToggleWindow();
        }

        private void SetupConfig()
        {
            #region Keyboard Shortcuts
            ToggleUI = Config.Bind(
                "Keyboard Shortcuts",
                "Toggle UI",
                new KeyboardShortcut(DEFAULT_TOGGLE_MAIN_KEY, DEFAULT_TOGGLE_MODIFIER),
                new ConfigDescription("Toggle the UI of ComponentUtil.",
                null,
                new ConfigurationManagerAttributes { Order = 1 }));
            #endregion Keyboard Shortcuts

            #region Config
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
            #endregion Config

            #region Config - UI
            ItemsPerPage = Config.Bind(
                "Config - UI",
                "Items per page",
                DEFAULT_ITEMS_PER_PAGE,
                new ConfigDescription("How many items to display in the transform / component list per page. Don't set this too high.",
                null,
                new ConfigurationManagerAttributes { Order = 2 }));

            UiScale = Config.Bind(
                "Config - UI",
                "UI scale",
                DEFAULT_UI_SCALE,
                new ConfigDescription("Scales the UI to given factor. Re-toggle ComponentUtil window for the change to apply.",
                null,
                new ConfigurationManagerAttributes { Order = 3 }));

            TransformWindowScale = Config.Bind(
                "Config - UI",
                "TransformList window scale",
                DEFAULT_WINDOW_SIZE_FACTOR,
                new ConfigDescription("Scale the TransformList window to given factors in width (X) and height (Y)." +
                " Re-toggle ComponentUtil window for the change to apply.",
                null,
                new ConfigurationManagerAttributes { Order = 1 }));

            ComponentWindowScale = Config.Bind(
                "Config - UI",
                "ComponentList window scale",
                DEFAULT_WINDOW_SIZE_FACTOR,
                new ConfigDescription("Scale the ComponentList window to given factors in width (X) and height (Y)." +
                " Re-toggle ComponentUtil window for the change to apply.",
                null,
                new ConfigurationManagerAttributes { Order = 1 }));

            ComponentAdderWindowScale = Config.Bind(
                "Config - UI",
                "ComponentAdder window scale",
                DEFAULT_WINDOW_SIZE_FACTOR,
                new ConfigDescription("Scale the ComponentAdder window to given factors in width (X) and height (Y)." +
                " Re-toggle ComponentUtil window for the change to apply.",
                null,
                new ConfigurationManagerAttributes { Order = 1 }));

            ComponentInspectorScale = Config.Bind(
                "Config - UI",
                "ComponentInspector window scale",
                DEFAULT_WINDOW_SIZE_FACTOR,
                new ConfigDescription("Scale the ComponentInspector window to given factors in width (X) and height (Y)." +
                " Re-toggle ComponentUtil window for the change to apply.",
                null,
                new ConfigurationManagerAttributes { Order = 1 }));

            ObjectInspectorScale = Config.Bind(
                "Config - UI",
                "ObjectInspector window scale",
                DEFAULT_WINDOW_SIZE_FACTOR,
                new ConfigDescription("Scale the ObjectInspector window to given factors in width (X) and height (Y)." +
                " Re-toggle ComponentUtil window for the change to apply.",
                null,
                new ConfigurationManagerAttributes { Order = 1 }));
            #endregion Config - UI
        }
    }
}
