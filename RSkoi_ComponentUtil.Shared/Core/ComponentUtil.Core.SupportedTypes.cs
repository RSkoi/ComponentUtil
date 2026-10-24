using System;
using System.Collections.Generic;
using UnityEngine;

namespace RSkoi_ComponentUtil
{
    public static class SupportedTypes
    {
        /// <summary>
        /// The property and field types ComponentUtil supports.
        /// </summary>
        public static readonly HashSet<Type> supportedTypes =
        [
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(bool),
            typeof(int),
            typeof(uint),
            typeof(short),
            typeof(ushort),
            typeof(long),
            typeof(ulong),
            typeof(byte),
            typeof(sbyte),
            typeof(nint),
            typeof(nuint),
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Quaternion),
            typeof(Color),
            typeof(string),
        ];

        /// <summary>
        /// The property and field types ComponentUtil supports that should be treated as reference types.
        /// </summary>
        public static readonly HashSet<Type> supportedTypesRewireAsReference =
        [
            typeof(ParticleSystem.MainModule),
            typeof(ParticleSystem.CollisionModule),
            typeof(ParticleSystem.ColorBySpeedModule),
            typeof(ParticleSystem.ColorOverLifetimeModule),
            typeof(ParticleSystem.CustomDataModule),
            typeof(ParticleSystem.EmissionModule),
            typeof(ParticleSystem.ExternalForcesModule),
            typeof(ParticleSystem.ForceOverLifetimeModule),
            typeof(ParticleSystem.InheritVelocityModule),
            typeof(ParticleSystem.LightsModule),
            typeof(ParticleSystem.LimitVelocityOverLifetimeModule),
            typeof(ParticleSystem.NoiseModule),
            typeof(ParticleSystem.RotationBySpeedModule),
            typeof(ParticleSystem.RotationOverLifetimeModule),
            typeof(ParticleSystem.ShapeModule),
            typeof(ParticleSystem.SizeBySpeedModule),
            typeof(ParticleSystem.SizeOverLifetimeModule),
            typeof(ParticleSystem.SubEmittersModule),
            typeof(ParticleSystem.TextureSheetAnimationModule),
            typeof(ParticleSystem.TrailModule),
            typeof(ParticleSystem.TriggerModule),
            typeof(ParticleSystem.VelocityOverLifetimeModule),
        ];

        /// <summary>
        /// The property and field types ComponentUtil explicitly does not support (blacklist).
        /// </summary>
        public static readonly HashSet<Type> blacklistTypes = [ ];
    }
}
