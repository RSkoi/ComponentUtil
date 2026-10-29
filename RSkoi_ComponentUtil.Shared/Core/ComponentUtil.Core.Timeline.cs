using System;
using System.Xml;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using Studio;
using KKAPI.Utilities;

using RSkoi_ComponentUtil.Core;
using RSkoi_ComponentUtil.Scene;
using static RSkoi_ComponentUtil.ComponentUtil;
using static RSkoi_ComponentUtil.ComponentUtilTracker;

namespace RSkoi_ComponentUtil.Timeline
{
    /// <summary>
    /// Heavily inspired by MaterialEditor.
    /// </summary>
    public static class ComponentUtilTimeline
    {
        internal const string INTERPOLABLE_ID = "ComponentUtil_PropEditValue";
        internal const string INTERPOLABLE_NAME = "Value";

        #region availability and refresh
        static bool? isTimelineAvailable;
        static MethodInfo refreshInterpolablesList;

        static Type GetKkapiType()
        {
#if KK
            return Type.GetType("KKAPI.Utilities.TimelineCompatibility,KKAPI", throwOnError: false);
#elif KKS
            return Type.GetType("KKAPI.Utilities.TimelineCompatibility,KKSAPI", throwOnError: false);
#else       // ComponentUtil doesn't support others
            return null;
#endif
        }

        internal static bool IsTimelineAvailable()
        {
            if (isTimelineAvailable == null)
            {
                var type = GetKkapiType();
                if (type != null)
                {
                    var _isTimelineAvailable = type.GetMethod("IsTimelineAvailable", BindingFlags.Static | BindingFlags.Public);
                    isTimelineAvailable = (bool)_isTimelineAvailable.Invoke(null, null);
                }
                else isTimelineAvailable = false;
            }
            return (bool)isTimelineAvailable;
        }

        internal static void RefreshInterpolablesList()
        {
            if (!(bool)isTimelineAvailable) return;

            if (refreshInterpolablesList == null)
            {
                var type = GetKkapiType();
                if (type != null)
                    refreshInterpolablesList = type.GetMethod("RefreshInterpolablesList", BindingFlags.Static | BindingFlags.Public);
            }
            refreshInterpolablesList.Invoke(null, null);
        }
        #endregion availability and refresh

        #region selection
        static SelectedTimelineModelTarget _selectedTimelineModelTarget;

        internal static void SelectTimelineModelTarget(PropertyKey key, Member member, PropertyTrackerData.PropertyTrackerDataOptions options)
        {
            _selectedTimelineModelTarget = new(
                key.ObjCtrlInfo,
                key.Go,
                key.Component,
                key.Component,
                member,
                null,
                member.Name,
                options);
            RefreshInterpolablesList();

            Logger.LogMessage($"Sent {key.Go.name}.{key.Component.GetType().Name}.{member.Name} to Timeline");
        }

        internal static void SelectTimelineModelTarget(
            PropertyReferenceKey key,
            object target,
            Member member,
            PropertyTrackerData.PropertyTrackerDataOptions options)
        {
            _selectedTimelineModelTarget = new(
                key.ObjCtrlInfo,
                key.Go,
                key.Component,
                target,
                member,
                key.Reference,
                member.Name,
                options | PropertyTrackerData.PropertyTrackerDataOptions.IsReference);
            RefreshInterpolablesList();

            Logger.LogMessage($"Sent {key.Go.name}.{key.Component.GetType().Name}.{key.Reference.Name}.{member.Name} to Timeline");
        }
        #endregion selection

        #region initialization and work
        readonly static Dictionary<SelectedTimelineModelTarget, TimelinePropertyParameter> _paramCache = [];

        #region internal
        /// <summary>
        /// Clears cache, resets selected model.
        /// </summary>
        internal static void ResetState()
        {
            _selectedTimelineModelTarget = null;
            _paramCache.Clear();
        }

        /// <summary>
        /// Deletes <b>oci</b> from cache if found, resets selected model, refreshes interpolable list.
        /// </summary>
        internal static void DeleteOciFromCache(ObjectCtrlInfo oci)
        {
            if (_selectedTimelineModelTarget != null && _selectedTimelineModelTarget.Oci == oci)
                _selectedTimelineModelTarget = null;

            foreach (var key in new List<SelectedTimelineModelTarget>(_paramCache.Keys))
                if (key.Oci == oci)
                    _paramCache.Remove(key);

            RefreshInterpolablesList();
        }

        /// <summary>
        /// Initializes Timeline stuff, call only once.
        /// </summary>
        internal static void Init()
        {
            if (!IsTimelineAvailable())
                return;

            TimelineCompatibility.AddInterpolableModelDynamic<string, TimelinePropertyParameter> (
                owner: PLUGIN_GUID,
                id: INTERPOLABLE_ID,
                name: INTERPOLABLE_NAME,
                interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => SetPropValue(parameter, leftValue, rightValue, factor),
                interpolateAfter: null,
                getParameter: GetPropertyParameter,
                getValue: (oci, parameter) => GetPropValue(parameter),
                readValueFromXml: (parameter, node) => node.Attributes["value"].Value,
                writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", value),
                readParameterFromXml: ReadPropertyParamXml,
                writeParameterToXml: WritePropertyParamXml,
                checkIntegrity: (oci, parameter, leftValue, rightValue) => true,
                isCompatibleWithTarget: IsCompatibleWithTarget,
                getFinalName: GetFinalInterpolableName
            );
        }
        #endregion internal

        #region set value
        static void SetPropValue(TimelinePropertyParameter param, string leftValue, string rightValue, float factor)
        {
            // don't lerp ints, strings and whatever else
            if (!IsInterpolatable(param.Type))
            {
                ValueConversion.SetValueWithFlags(param.Member, param.Target, leftValue, param.Options);
                return;
            }

            object left, right;
            try
            {
                left = ValueConversion.Decode(leftValue, param.Type, param.Options);
                right = ValueConversion.Decode(rightValue, param.Type, param.Options);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                return;
            }
            var interpolatedValue = InterpolateValue(left, right, param.Type, factor);
            ValueConversion.SetValueWithFlags(param.Member, param.Target, interpolatedValue, param.Options);
        }

        static bool IsInterpolatable(Type t)
            => t == typeof(Color) || t == typeof(float) || VectorConversion.IsVectorType(t);

        static object InterpolateValue(object leftValue, object rightValue, Type t, float factor)
        {
            if (t == typeof(Color))
                return Color.LerpUnclamped((Color)leftValue, (Color)rightValue, factor);
            else if (t == typeof(Vector2))
                return Vector2.LerpUnclamped((Vector2)leftValue, (Vector2)rightValue, factor);
            else if (t == typeof(Vector3))
                return Vector3.LerpUnclamped((Vector3)leftValue, (Vector3)rightValue, factor);
            else if (t == typeof(Vector4))
                return Vector4.LerpUnclamped((Vector4)leftValue, (Vector4)rightValue, factor);
            else if (t == typeof(Quaternion))
                return Quaternion.LerpUnclamped((Quaternion)leftValue, (Quaternion)rightValue, factor);
            return Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor);
        }
        #endregion set value

        #region get value
        static string GetPropValue(TimelinePropertyParameter param)
            => ValueConversion.EncodeToString(param.Member.GetValue(param.Target), param.Options);

        static TimelinePropertyParameter GetPropertyParameter(ObjectCtrlInfo _)
        {
            var selected = _selectedTimelineModelTarget;
            if (selected == null)
            {
                Logger.LogError("GetPropertyParameter: no property was sent to Timeline");
                return null;
            }

            if (_paramCache.TryGetValue(selected, out var cached))
                return cached;

            var param = new TimelinePropertyParameter(
                selected.Oci,
                selected.Go,
                selected.Component,
                selected.Target,
                selected.Member,
                selected.Reference,
                selected.PropertyName,
                selected.Options);
            _paramCache.Add(selected, param);
            return param;
        }

        static string GetFinalInterpolableName(string _, ObjectCtrlInfo __, TimelinePropertyParameter param)
            => $"{param.Target.GetType().Name}.{param.PropertyName}";
        #endregion get value

        #region xml write and read
        static void WritePropertyParamXml(ObjectCtrlInfo oci, XmlTextWriter writer, TimelinePropertyParameter param)
        {
            writer.WriteAttributeString("goPath", ComponentUtilSceneBehaviour.GetGameObjectPathToRoot(param.Go.transform, oci.guideObject.transformTarget));
            writer.WriteAttributeString("componentName", param.Component.GetType().Name);
            if (param.Reference is MemberId reference)
            {
                writer.WriteAttributeString("referenceName", reference.Name);
                writer.WriteAttributeString("referenceIsProperty", reference.IsProperty.ToString());
            }
            writer.WriteAttributeString("propertyName", param.PropertyName);
            writer.WriteAttributeString("options", param.Options.ToString());
        }

        static TimelinePropertyParameter ReadPropertyParamXml(ObjectCtrlInfo oci, XmlNode node)
        {
            var goPath = node.Attributes["goPath"].Value;
            var componentName = node.Attributes["componentName"].Value;
            var options = (PropertyTrackerData.PropertyTrackerDataOptions)Enum.Parse(
                typeof(PropertyTrackerData.PropertyTrackerDataOptions),
                node.Attributes["options"].Value);
            bool isProperty = HasPropertyFlag(options, PropertyTrackerData.PropertyTrackerDataOptions.IsProperty);
            bool isInsideReference = HasPropertyFlag(options, PropertyTrackerData.PropertyTrackerDataOptions.IsReference);
            var propertyName = node.Attributes["propertyName"].Value;

            var providedGameObjectTarget = oci.guideObject.transformTarget;
            var goTransform = string.IsNullOrEmpty(goPath) ? providedGameObjectTarget : providedGameObjectTarget.Find(goPath);
            if (goTransform == null)
                throw new InvalidOperationException($"ReadPropertyParamXml: could not find GameObject {goPath}");
            var go = goTransform.gameObject;

            var component = go.GetComponentByName(componentName);
            if (component == null)
                throw new InvalidOperationException($"ReadPropertyParamXml: could not find component {componentName} on {go.name}");
            var componentType = component.GetType();
            object target = component;

            MemberId? referenceId = null;
            if (isInsideReference)
            {
                var referenceName = node.Attributes["referenceName"].Value;
                var referenceIsProperty = node.Attributes["referenceIsProperty"]; // null on older scenes
                var reference = (referenceIsProperty == null
                    ? Member.FindAny(componentType, referenceName)
                    : Member.Find(componentType, referenceName, bool.Parse(referenceIsProperty.Value)))
                    ?? throw new InvalidOperationException($"ReadPropertyParamXml: could not find reference type {componentName}.{referenceName}");

                referenceId = reference.Id;
                target = reference.TryGetValue(component)
                    ?? throw new InvalidOperationException($"ReadPropertyParamXml: could not find reference value of {componentName}.{referenceName}");

                componentType = target.GetType();
            }
            var member = Member.Find(componentType, propertyName, isProperty)
                ?? throw new InvalidOperationException($"ReadPropertyParamXml: could not find {new MemberId(propertyName, isProperty)} on {componentType.Name}");

            var param = new TimelinePropertyParameter(
                oci,
                go,
                component,
                target,
                member,
                referenceId,
                propertyName,
                options);
            return param;
        }
        #endregion xml write and read

        static bool IsCompatibleWithTarget(ObjectCtrlInfo oci)
        {
            // why does this happen?
            if (oci == null)
                return false;

            // no timeline button of a property was clicked yet
            if (_selectedTimelineModelTarget == null)
                return false;

            // all actual items in the studio workspace are supported
            return true;
        }

        #region classes
        class TimelinePropertyParameter(
            ObjectCtrlInfo oci,
            GameObject go,
            Component component,
            object target,
            Member member,
            MemberId? reference,
            string propertyName,
            PropertyTrackerData.PropertyTrackerDataOptions options)
        {
            public ObjectCtrlInfo Oci = oci;
            public GameObject Go = go;
            public Component Component = component;
            public object Target = target;
            public Member Member = member;
            public Type Type = member.MemberType;
            public MemberId? Reference = reference;
            public string PropertyName = propertyName;
            public PropertyTrackerData.PropertyTrackerDataOptions Options = options;

            public override string ToString()
            {
                return $"TimelinePropertyParameter [ Oci: {Oci}, Go: {Go.name}, Target: {Target}," +
                    $" Member: {Member}, Type: {Type}, PropertyName: {PropertyName}, Options: {Options} ]";
            }
        }

        class SelectedTimelineModelTarget(
            ObjectCtrlInfo oci,
            GameObject go,
            Component component,
            object target,
            Member member,
            MemberId? reference,
            string propertyName,
            PropertyTrackerData.PropertyTrackerDataOptions options)
            : IEquatable<SelectedTimelineModelTarget>
        {
            public ObjectCtrlInfo Oci = oci;
            public GameObject Go = go;
            public Component Component = component;
            public object Target = target;
            public Member Member = member;
            public MemberId? Reference = reference;
            public string PropertyName = propertyName;
            public PropertyTrackerData.PropertyTrackerDataOptions Options = options;

            public override int GetHashCode()
            {
                // https://stackoverflow.com/questions/1646807/quick-and-simple-hash-code-combinations
                unchecked
                {
                    var hash = 17;
                    hash = hash * 31 + (Oci?.GetHashCode() ?? 0);
                    hash = hash * 31 + (Go != null ? Go.GetHashCode() : 0);
                    hash = hash * 31 + (Target?.GetHashCode() ?? 0);
                    hash = hash * 31 + (PropertyName?.GetHashCode() ?? 0);
                    hash = hash * 31 + (Member?.Id.GetHashCode() ?? 0);
                    return hash;
                }
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as SelectedTimelineModelTarget);
            }

            public bool Equals(SelectedTimelineModelTarget other)
            {
                return other != null
                    && Oci          == other.Oci
                    && Go           == other.Go
                    && Component    == other.Component
                    && Target       == other.Target
                    && PropertyName == other.PropertyName
                    && Equals(Member?.Id, other.Member?.Id);
            }

            public override string ToString()
            {
                return $"SelectedTimelineModelTarget [ Oci: {Oci}, Go: {Go.name}, Target: {Target}," +
                    $" Member: {Member}, PropertyName: {PropertyName}, Options: {Options} ]";
            }
        }
        #endregion classes
        #endregion initialization and work
    }
}
