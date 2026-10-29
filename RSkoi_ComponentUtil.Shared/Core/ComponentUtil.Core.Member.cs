using System;
using System.Reflection;

using static RSkoi_ComponentUtil.PropertyTrackerData;

namespace RSkoi_ComponentUtil.Core
{
    internal class Member
    {
        internal const BindingFlags INSTANCE_MEMBERS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal readonly PropertyInfo Property;
        internal readonly FieldInfo Field;

        internal bool IsProperty                        => Property != null;
        internal string Name                            => IsProperty ? Property.Name : Field.Name;
        internal MemberId Id                            => new(Name, IsProperty);
        internal Type MemberType                        => IsProperty ? Property.PropertyType : Field.FieldType;
        internal bool HasPublicSetter                   => !IsProperty || Property.GetSetMethod() != null;
        internal PropertyTrackerDataOptions MemberFlag  => Id.MemberFlag;

        internal string KindName    => Id.KindName;
        internal string Label       => IsProperty ? $"[Property] {Name}" : $"[Field] {Name}";

        Member(PropertyInfo property, FieldInfo field)
        {
            Property = property;
            Field = field;
        }

        internal static Member Of(PropertyInfo prop)    => new (prop, null);
        internal static Member Of(FieldInfo field)      => new (null, field);

        /// <summary>
        /// Finds an instance property or field by name.
        /// </summary>
        /// <returns>property or field as member, or null</returns>
        internal static Member Find(Type type, string memberName, bool isProperty)
        {
            if (isProperty)
            {
                var property = FindProperty(type, memberName);
                return property == null ? null : Of(property);
            }

            var field = type.GetField(memberName, INSTANCE_MEMBERS);
            return field == null ? null : Of(field);
        }

        /// <summary>
        /// Finds an instance property or field (if property wasn't found) by name.
        /// </summary>
        /// <returns>the member or null if neither exists</returns>
        internal static Member FindAny(Type type, string memberName)
            => Find(type, memberName, isProperty: true)
            ?? Find(type, memberName, isProperty: false);

        static PropertyInfo FindProperty(Type type, string memberName)
        {
            try
            {
                return type.GetProperty(memberName, INSTANCE_MEMBERS);
            }
            catch (AmbiguousMatchException)
            {
                // e.g. hidden with 'new'
                Logger.LogWarning("Found multiple properties with identical name, trying to resolve public one");
            }

            try
            {
                var publicProperty = type.GetProperty(memberName);
                if (publicProperty != null)
                    return publicProperty;
            }
            catch (AmbiguousMatchException)
            {
                Logger.LogWarning("Found multiple public properties with identical name, trying to resolve last one in inheritance tree");
            }

            for (var declaringType = type; declaringType != null; declaringType = declaringType.BaseType)
                foreach (var property in declaringType.GetProperties(INSTANCE_MEMBERS | BindingFlags.DeclaredOnly))
                    if (property.Name == memberName && property.GetIndexParameters().Length == 0) // most derived one that's not an indexer
                        return property;

            return null;
        }

        internal object GetValue(object target)
            => IsProperty ? Property.GetValue(target, null) : Field.GetValue(target);

        internal void SetValue(object target, object value)
        {
            if (IsProperty)
                Property.SetValue(target, value, null);
            else
                Field.SetValue(target, value);
        }

        internal object TryGetValue(object target)
        {
            try
            {
                return GetValue(target);
            }
            catch (Exception e)
            {
                Logger.LogError($"Could not get value of {Id} on {target}: {e}");
                return null;
            }
        }

        public override string ToString() => Id.ToString();
    }

    internal readonly record struct MemberId(string Name, bool IsProperty)
    {
        public PropertyTrackerDataOptions MemberFlag    => IsProperty ? PropertyTrackerDataOptions.IsProperty : PropertyTrackerDataOptions.None;
        public string KindName                          => IsProperty ? "property" : "field";

        public override int GetHashCode()
        {
            unchecked
            {
                return (Name?.GetHashCode() ?? 0) * 31
                    + IsProperty.GetHashCode();
            }
        }

        public override string ToString() => $"{KindName} {Name}";
    }
}
