using System;
using System.Collections.Generic;

using RSkoi_ComponentUtil.Scripts;

namespace RSkoi_ComponentUtil
{
    public partial class ComponentUtil
    {
        /// <summary>
        /// the redirector types ComponentUtil provides
        /// </summary>
        internal static readonly Dictionary<ComponentUtilRedirectorType, Type> redirectorTypes = new()
        {
            { ComponentUtilRedirectorType.LightmapSettingsRedirector,   typeof(LightmapSettingsRedirector) },
            { ComponentUtilRedirectorType.QualitySettingsRedirector,    typeof(QualitySettingsRedirector) },
            { ComponentUtilRedirectorType.RenderSettingsRedirector,     typeof(RenderSettingsRedirector) },
        };
    }

    internal enum ComponentUtilRedirectorType
    {
        LightmapSettingsRedirector,
        QualitySettingsRedirector,
        RenderSettingsRedirector,
    }
}
