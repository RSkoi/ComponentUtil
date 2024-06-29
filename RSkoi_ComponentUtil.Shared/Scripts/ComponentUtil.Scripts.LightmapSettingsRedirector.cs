using UnityEngine;

namespace RSkoi_ComponentUtil.Scripts
{
    public class LightmapSettingsRedirector : MonoBehaviour
    {
        public LightmapData[] Lightmaps
        {
            get { return LightmapSettings.lightmaps; }
            set { LightmapSettings.lightmaps = value; }
        }

        public LightmapsMode LightmapsMode
        {
            get { return LightmapSettings.lightmapsMode; }
            set { LightmapSettings.lightmapsMode = value; }
        }

        public LightProbes LightProbes
        {
            get { return LightmapSettings.lightProbes; }
            set { LightmapSettings.lightProbes = value; }
        }

        public void Start()
        {
            ComponentUtil._logger.LogInfo("LightmapSettingsRedirector started");
        }
    }
}
