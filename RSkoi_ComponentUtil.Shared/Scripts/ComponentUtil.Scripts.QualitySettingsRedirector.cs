using UnityEngine;

namespace RSkoi_ComponentUtil.Scripts
{
    public class QualitySettingsRedirector : MonoBehaviour
    {
        public ColorSpace ActiveColorSpace
        { 
            get { return QualitySettings.activeColorSpace; }
            private set { }
        }

        public AnisotropicFiltering AnisotropicFiltering
        {
            get { return QualitySettings.anisotropicFiltering; }
            set { QualitySettings.anisotropicFiltering = value; }
        }

        public int AntiAliasing
        {
            get { return QualitySettings.antiAliasing; }
            set { QualitySettings.antiAliasing = value; }
        }

        public int AsyncUploadBufferSize
        {
            get { return QualitySettings.asyncUploadBufferSize; }
            set { QualitySettings.asyncUploadBufferSize = value; }
        }

        public int AsyncUploadTimeSlice
        {
            get { return QualitySettings.asyncUploadTimeSlice; }
            set { QualitySettings.asyncUploadTimeSlice = value; }
        }

        public bool BillboardsFaceCameraPosition
        {
            get { return QualitySettings.billboardsFaceCameraPosition; }
            set { QualitySettings.billboardsFaceCameraPosition = value; }
        }

#if KK
        public BlendWeights BlendWeights
        {
            get { return QualitySettings.blendWeights; }
            set { QualitySettings.blendWeights = value; }
        }
#elif KKS
        public SkinWeights SkinWeights
        {
            get { return QualitySettings.skinWeights; }
            set { QualitySettings.skinWeights = value; }
        }
#endif

        public ColorSpace DesiredColorSpace
        {
            get { return QualitySettings.desiredColorSpace; }
            private set { }
        }

        public float LodBias
        {
            get { return QualitySettings.lodBias; }
            set { QualitySettings.lodBias = value; }
        }

        public int MasterTextureLimit
        {
            get { return QualitySettings.masterTextureLimit; }
            set { QualitySettings.masterTextureLimit = value; }
        }

        public int MaximumLODLevel
        {
            get { return QualitySettings.maximumLODLevel; }
            set { QualitySettings.maximumLODLevel = value; }
        }

        public int MaxQueuedFrames
        {
            get { return QualitySettings.maxQueuedFrames; }
            set { QualitySettings.maxQueuedFrames = value; }
        }

        public string[] Names
        {
            get { return QualitySettings.names; }
            private set { }
        }

        public int ParticleRaycastBudget
        {
            get { return QualitySettings.particleRaycastBudget; }
            set { QualitySettings.particleRaycastBudget = value; }
        }

        public int PixelLightCount
        {
            get { return QualitySettings.pixelLightCount; }
            set { QualitySettings.pixelLightCount = value; }
        }

        public bool RealtimeReflectionProbes
        {
            get { return QualitySettings.realtimeReflectionProbes; }
            set { QualitySettings.realtimeReflectionProbes = value; }
        }

        public float ShadowCascade2Split
        {
            get { return QualitySettings.shadowCascade2Split; }
            set { QualitySettings.shadowCascade2Split = value; }
        }

        public Vector3 ShadowCascade4Split
        {
            get { return QualitySettings.shadowCascade4Split; }
            set { QualitySettings.shadowCascade4Split = value; }
        }

        public int ShadowCascades
        {
            get { return QualitySettings.shadowCascades; }
            set { QualitySettings.shadowCascades = value; }
        }

        public float ShadowDistance
        {
            get { return QualitySettings.shadowDistance; }
            set { QualitySettings.shadowDistance = value; }
        }

        public float ShadowNearPlaneOffset
        {
            get { return QualitySettings.shadowNearPlaneOffset; }
            set { QualitySettings.shadowNearPlaneOffset = value; }
        }

        public ShadowProjection ShadowProjection
        {
            get { return QualitySettings.shadowProjection; }
            set { QualitySettings.shadowProjection = value; }
        }

        public ShadowResolution ShadowResolution
        {
            get { return QualitySettings.shadowResolution; }
            set { QualitySettings.shadowResolution = value; }
        }

        public ShadowQuality Shadows
        {
            get { return QualitySettings.shadows; }
            set { QualitySettings.shadows = value; }
        }

        public bool SoftParticles
        {
            get { return QualitySettings.softParticles; }
            set { QualitySettings.softParticles = value; }
        }

        public bool SoftVegetation
        {
            get { return QualitySettings.softVegetation; }
            set { QualitySettings.softVegetation = value; }
        }

        public int VSyncCount
        {
            get { return QualitySettings.vSyncCount; }
            set { QualitySettings.vSyncCount = value; }
        }

        public void Start()
        {
            ComponentUtil._logger.LogInfo("QualitySettingsRedirector started");
        }
    }
}
