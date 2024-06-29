using UnityEngine;
using UnityEngine.Rendering;

namespace RSkoi_ComponentUtil.Scripts
{
    public class RenderSettingsRedirector : MonoBehaviour
    {
        public Color AmbientEquatorColor
        {
            get { return RenderSettings.ambientEquatorColor; }
            set { RenderSettings.ambientEquatorColor = value; }
        }

        public Color AmbientGroundColor
        {
            get { return RenderSettings.ambientGroundColor; }
            set { RenderSettings.ambientGroundColor = value; }
        }

        public float AmbientIntensity
        {
            get { return RenderSettings.ambientIntensity; }
            set { RenderSettings.ambientIntensity = value; }
        }

        public Color AmbientLight
        {
            get { return RenderSettings.ambientLight; }
            set { RenderSettings.ambientLight = value; }
        }

        public AmbientMode AmbientMode
        {
            get { return RenderSettings.ambientMode; }
            set { RenderSettings.ambientMode = value; }
        }

        public SphericalHarmonicsL2 AmbientProbe
        {
            get { return RenderSettings.ambientProbe; }
            set { RenderSettings.ambientProbe = value; }
        }

        public Color AmbientSkyColor
        {
            get { return RenderSettings.ambientSkyColor; }
            set { RenderSettings.ambientSkyColor = value; }
        }

        public Cubemap CustomReflection
        {
            get { return RenderSettings.customReflection; }
            set { RenderSettings.customReflection = value; }
        }

        public DefaultReflectionMode DefaultReflectionMode
        {
            get { return RenderSettings.defaultReflectionMode; }
            set { RenderSettings.defaultReflectionMode = value; }
        }

        public int DefaultReflectionResolution
        {
            get { return RenderSettings.defaultReflectionResolution; }
            set { RenderSettings.defaultReflectionResolution = value; }
        }

        public float FlareFadeSpeed
        {
            get { return RenderSettings.flareFadeSpeed; }
            set { RenderSettings.flareFadeSpeed = value; }
        }

        public float FlareStrength
        {
            get { return RenderSettings.flareStrength; }
            set { RenderSettings.flareStrength = value; }
        }

        public bool Fog
        {
            get { return RenderSettings.fog; }
            set { RenderSettings.fog = value; }
        }

        public Color FogColor
        {
            get { return RenderSettings.fogColor; }
            set { RenderSettings.fogColor = value; }
        }

        public float FogDensity
        {
            get { return RenderSettings.fogDensity; }
            set { RenderSettings.fogDensity = value; }
        }

        public float FogEndDistance
        {
            get { return RenderSettings.fogEndDistance; }
            set { RenderSettings.fogEndDistance = value; }
        }

        public FogMode FogMode
        {
            get { return RenderSettings.fogMode; }
            set { RenderSettings.fogMode = value; }
        }

        public float FogStartDistance
        {
            get { return RenderSettings.fogStartDistance; }
            set { RenderSettings.fogStartDistance = value; }
        }

        public float HaloStrength
        {
            get { return RenderSettings.haloStrength; }
            set { RenderSettings.haloStrength = value; }
        }

        public int ReflectionBounces
        {
            get { return RenderSettings.reflectionBounces; }
            set { RenderSettings.reflectionBounces = value; }
        }

        public float ReflectionIntensity
        {
            get { return RenderSettings.reflectionIntensity; }
            set { RenderSettings.reflectionIntensity = value; }
        }

        public Material Skybox
        {
            get { return RenderSettings.skybox; }
            set { RenderSettings.skybox = value; }
        }

        public Color SubtractiveShadowColor
        {
            get { return RenderSettings.subtractiveShadowColor; }
            set { RenderSettings.subtractiveShadowColor = value; }
        }

        public Light Sun
        {
            get { return RenderSettings.sun; }
            set { RenderSettings.sun = value; }
        }

        public void Start()
        {
            ComponentUtil._logger.LogInfo("RenderSettingsRedirector started");
        }
    }
}
