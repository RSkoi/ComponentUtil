using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using RSkoi_ComponentUtil.UI;
using RSkoi_ComponentUtil.Core;

using static RSkoi_ComponentUtil.ComponentUtilTracker;
using static RSkoi_ComponentUtil.SupportedTypes;

namespace RSkoi_ComponentUtil
{
    internal static partial class ComponentUtilEditor
    {
        /// <summary>
        /// Sets up component inspector: configure delete component button,
        /// get all fields of currently selected component, map to list entries of different types.
        /// </summary>
        /// <param name="input">selected component to list the properties and fields of</param>
        internal static void GetAllFieldsAndProperties(Component input)
        {
            // clear the inspector, so nothing of a previous selection stays visible and editable
            ComponentUtilUI._componentDeleteButton.interactable = false;
            ComponentUtilUI._componentDeleteButton.onClick.RemoveAllListeners();
            ComponentUtilUI.ResetAndDisablePropertyEntries();
            _shownReferenceEntries.Clear();
            ComponentUtilUI.UpdateUISelectedText(ComponentUtilUI._componentPropertyListSelectedComponentText, "None");
            ComponentUtilUI.UpdateUISelectedText(ComponentUtilUI._objectPropertyListSelectedText, "None");
            _selectedReferencePropertyUiEntry = null;

            // otherwise the color picker from a color prop can produce a null ref
            if (Studio.Studio.Instance.colorPalette.visible)
                Studio.Studio.Instance.colorPalette.visible = false;

            // otherwise changing value in ObjectInspector will fail with exception
            if (ComponentUtilUI._objectInspectorWindow.gameObject.activeSelf)
                ComponentUtilUI._objectInspectorWindow.gameObject.SetActive(false);

            if (input == null)
                return;

            #region component delete button
            if (ComponentIsTracked(_selectedObject, input.gameObject, input.GetType().FullName))
            {
                ComponentUtilUI._componentDeleteButton.interactable = true;
                ComponentUtilUI._componentDeleteButton.onClick.AddListener(() =>
                {
                    var go = input.gameObject;
                    if (RemoveComponentFromTracker(_selectedObject, go, input))
                        UnityEngine.Object.DestroyImmediate(input); // scary, so that GetOrCacheComponents doesn't pick it up again

                    // force refresh the component list cache of the GO
                    ComponentUtilCache.GetOrCacheComponents(go, forceRefresh: true);

                    Entry(_selectedObject);
                });
            }
            #endregion component delete button

            ComponentUtilUI.UpdateUISelectedText(
                ComponentUtilUI._componentPropertyListSelectedComponentText,
                $"{input.gameObject.name}.{input.GetType().Name}");

            var filter = ComponentUtilUI.PageSearchComponentInspectorInputValue.ToLower();
            foreach (var p in ComponentUtilCache.GetOrCachePropertyInfos(input))
            {
                // indexed properties are annoying
                if (p.GetIndexParameters().Length != 0)
                    continue;

                // ignore properties with private or missing getters
                if (p.GetGetMethod() == null)
                    continue;

                // collections are annoying
                if (typeof(IEnumerable).IsAssignableFrom(p.PropertyType)
                    && !supportedTypes.Contains(p.PropertyType))
                    continue;

                // filter string
                if (!string.IsNullOrEmpty(filter) && !p.Name.ToLower().Contains(filter))
                    continue;

                ConfigurePropertyEntry(input, Member.Of(p));
            }

            foreach (var f in ComponentUtilCache.GetOrCacheFieldInfos(input))
            {
                // collections are annoying
                if (typeof(IEnumerable).IsAssignableFrom(f.FieldType)
                    && !supportedTypes.Contains(f.FieldType))
                    continue;

                // filter string
                if (!string.IsNullOrEmpty(filter) && !f.Name.ToLower().Contains(filter))
                    continue;

                ConfigurePropertyEntry(input, Member.Of(f));
            }
        }

        /// <param name="input">the selected Component (ComponentInspector), or a referenced object (ObjectInspector)</param>
        /// <param name="member">the property / field of input to create an entry for</param>
        /// <param name="objectMode">whether we're populating the ObjectInspector (true) or ComponentInspector (false)</param>
        static void ConfigurePropertyEntry(object input, Member member, bool objectMode = false)
        {
            if (input == null)
            {
                Logger.LogError($"ConfigurePropertyEntry: received input null value for {member}");
                return;
            }

            // input cast to Component in the ComponentInspector
            var cInput = objectMode ? null : (Component)input;
            if (!objectMode && cInput == null)
            {
                Logger.LogError("ConfigurePropertyEntry: objectMode is false, but could not convert input to Component");
                return;
            }

            var type = member.MemberType;
            if (blacklistTypes.Contains(type)
                || !type.IsEnum && type.IsValueType && !supportedTypes.Contains(type) && !supportedTypesRewireAsReference.Contains(type))
                return;

            var value = member.TryGetValue(input);
            if (value == null)
            {
                SetupNullEntry(member, objectMode);
                return;
            }

            var kind = EntryKinds.Of(type);
            // don't recurse down from the ObjectInspector
            if (objectMode && kind == EntryKind.Reference)
                return;

            var uiEntry = ComponentUtilUI.GetOrInstantiatePropEntryFromPool(ComponentUtilUI.MapEntryKindToPrefab(kind), objectMode);
            uiEntry.PropertyName.text = member.Label;
            uiEntry.MemberName = member.Name;

            var ctx = new PropertyEntryContext(uiEntry, input, member, value, kind.ToTrackerFlag() | member.MemberFlag, objectMode);
            switch (kind)
            {
                case EntryKind.Reference:
                    ConfigReference(ctx);
                    break;
                case EntryKind.Enum:
                    ConfigDropdown(ctx);
                    break;
                case EntryKind.Bool:
                    ConfigToggle(ctx);
                    break;
                case EntryKind.Vector:
                    if (!ConfigVector(ctx))
                    {
                        var owner = objectMode ? input.ToString() : $"{cInput.transform.name}.{cInput.name}";
                        Logger.LogWarning($"Could not configure vector property entry on {owner} with name {member.Name}, skipping entry");
                        // the entry is pooled and still inactive, leave it in the pool instead of destroying it
                        return;
                    }
                    break;
                case EntryKind.Color:
                    ConfigColor(ctx);
                    break;
                default:
                    ConfigInput(ctx, MapTypeToInputContentType(type));
                    break;
            }

            if (kind == EntryKind.Reference) // reference type, only in the component inspector
            {
                // make green if dummy property entry is tracked
                var key = SelectedPropertyKey(cInput);
                if (PropertyIsTracked(key, member.Name))
                    uiEntry.SetBgColorEdited();
            }
            else if (objectMode) // property inside reference type / the object inspector
            {
                var key = SelectedPropertyReferenceKey(_selectedComponent, _selectedReferencePropertyUiEntry.MemberName);
                if (PropertyIsTracked(key, member.Name))
                    MarkAsEditedIfChanged(ctx, GetTrackedDefaultValue(key, member.Name));

                ConfigResetReferenceProperty(key, ctx);
                ConfigTimelineReferenceProperty(key, ctx);
            }
            else // property in the component inspector
            {
                var key = SelectedPropertyKey(cInput);
                if (PropertyIsTracked(key, member.Name))
                    MarkAsEditedIfChanged(ctx, GetTrackedDefaultValue(key, member.Name));

                ConfigReset(key, ctx);
                ConfigTimeline(key, ctx);
            }

            uiEntry.UiGO.SetActive(true);
        }

        static void SetupNullEntry(Member member, bool objectMode)
        {
            var uiNullEntry = ComponentUtilUI.GetOrInstantiatePropEntryFromPool(ComponentUtilUI.NullEntryPrefab, objectMode);

            uiNullEntry.PropertyName.text = member.Label;
            uiNullEntry.MemberName = member.Name;
            uiNullEntry.ResetButton.interactable = false;
            uiNullEntry.TimelineButton.interactable = false;

            uiNullEntry.UiGO.SetActive(true);
        }

        /// <param name="defaultValue">tracked default value of the member, encoded (see ValueConversion)</param>
        static void MarkAsEditedIfChanged(PropertyEntryContext ctx, object defaultValue)
        {
            if (defaultValue == null)
            {
                Logger.LogError($"MarkAsEditedIfChanged: no defaultValue for property {ctx.Member.Name} found or value is null");
                return;
            }

            // compare encoded versions, current value is not encoded yet
            if (InvariantConversion.ToInvariantString(defaultValue) != ValueConversion.EncodeToString(ctx.Value, ctx.Flags))
                ctx.UiEntry.SetBgColorEdited();
        }

        static InputField.ContentType MapTypeToInputContentType(Type type)
        {
            if (type.Equals(typeof(string)))
                return InputField.ContentType.Standard;
            else if (InvariantConversion.IsFloatingPoint(type))
                return InputField.ContentType.DecimalNumber;
            else
                return InputField.ContentType.IntegerNumber; // trying to cram an integer into e.g. a short will lead to problems
        }

        static void RebuildComponentInspector()
        {
            var openReference = ObjectInspectorIsOpen ? _selectedReferenceMember : null;

            // closes the ObjectInspector
            GetAllFieldsAndProperties(_selectedComponent);

            if (openReference == null)
                return;
            if (_shownReferenceEntries.TryGetValue(openReference.Name, out var referenceEntry))
                OpenObjectInspector(openReference, openReference.TryGetValue(_selectedComponent), referenceEntry);
        }

        #region pages
        internal static void OnFilterComponentInspector()
        {
            if (_selectedComponent == null)
                return;

            GetAllFieldsAndProperties(_selectedComponent);

            ComponentUtilUI.TraverseAndSetEditedParents();
        }

        internal static void OnRefreshComponentInspector()
        {
            if (_selectedComponent == null)
                return;

            ComponentUtilCache.GetOrCachePropertyInfos(_selectedComponent, forceRefresh: true);
            ComponentUtilCache.GetOrCacheFieldInfos(_selectedComponent, forceRefresh: true);
            GetAllFieldsAndProperties(_selectedComponent);

            Logger.LogMessage("Force refreshed ComponentInspector cache");

            ComponentUtilUI.TraverseAndSetEditedParents();
        }
        #endregion pages

        #region copy / pasting edits
        /// <summary>
        /// Whether there is a component (and in the ObjectInspector a reference) selected to copy from / paste onto.
        /// </summary>
        static bool HasCopyPasteTarget(bool objectMode)
            => _selectedComponent != null && (!objectMode || _selectedReferencePropertyUiEntry != null);

        internal static void OnCopyEdits(bool objectMode = false)
        {
            if (!HasCopyPasteTarget(objectMode))
                return;

            Dictionary<string, PropertyTrackerData> propEdits;
            object source;
            string sourceName;
            if (!objectMode)
            {
                var key = SelectedPropertyKey(_selectedComponent);
                _propertyTracker.TryGetValue(key, out propEdits);
                source = _selectedComponent;
                sourceName = $"{_selectedComponent.gameObject.name}.{_selectedComponent.GetType().Name}";
            }
            else
            {
                var refKey = SelectedPropertyReferenceKey(_selectedComponent, _selectedReferencePropertyUiEntry.MemberName);
                _referencePropertyTracker.TryGetValue(refKey, out propEdits);
                source = _selectedReferencePropertyUiEntry.Wrapper;
                sourceName = $"{_selectedComponent.gameObject.name}.{_selectedComponent.GetType().Name}.{_selectedReferencePropertyUiEntry.MemberName}";
            }

            if (propEdits == null)
            {
                Logger.LogMessage("No property or field edits to copy");
                return;
            }

            _currentCopiedEdits = new(GetEncodedValuesOfObj(source, propEdits));

            ComponentUtilUI._componentPropertyPasteButton.interactable = true;
            ComponentUtilUI._objectPropertyPasteButton.interactable = true;

            Logger.LogMessage($"Copied property and field edits of {sourceName}");
        }

        /// <returns>current encoded values of the tracked members of obj</returns>
        static Dictionary<string, CopiedEditValueTuple> GetEncodedValuesOfObj(object obj, Dictionary<string, PropertyTrackerData> trackerData)
        {
            var values = new Dictionary<string, CopiedEditValueTuple>();
            foreach (var pair in trackerData)
            {
                var flags = pair.Value.OptionFlags;
                if (HasPropertyFlag(flags, PropertyTrackerData.PropertyTrackerDataOptions.IsReference))
                    continue;

                var member = Member.Find(obj.GetType(), pair.Key, HasPropertyFlag(flags, PropertyTrackerData.PropertyTrackerDataOptions.IsProperty));
                var value = ValueConversion.Encode(member?.TryGetValue(obj), flags);
                if (value == null)
                    continue;

                values.Add(pair.Key, new(value, flags));
            }
            return values;
        }

        internal static void OnPasteEdits(bool objectMode = false)
        {
            if (_currentCopiedEdits == null)
            {
                ComponentUtilUI._componentPropertyPasteButton.interactable = false;
                ComponentUtilUI._objectPropertyPasteButton.interactable = false;
                return;
            }
            if (!HasCopyPasteTarget(objectMode))
                return;

            if (objectMode)
            {
                var refKey = SelectedPropertyReferenceKey(_selectedComponent, _selectedReferencePropertyUiEntry.MemberName);
                var pastedCount = PasteEdits(_selectedReferencePropertyUiEntry.Wrapper, (propName, defaultValue, flags) => AddPropertyToTracker(refKey, propName, defaultValue, flags));
                if (pastedCount > 0)
                    _selectedReferencePropertyUiEntry.SetBgColorEdited();
                RebuildObjectInspector();
            }
            else
            {
                var key = SelectedPropertyKey(_selectedComponent);
                PasteEdits(_selectedComponent, (propName, defaultValue, flags) => AddPropertyToTracker(key, propName, defaultValue, flags));
                RebuildComponentInspector();
            }

            ComponentUtilUI.TraverseAndSetEditedParents();
        }

        /// <param name="target">the component or referenced object to paste the edits onto</param>
        /// <param name="addToTracker">adds an edit to the tracker: member name, encoded default value, flags</param>
        static int PasteEdits(object target, Action<string, object, PropertyTrackerData.PropertyTrackerDataOptions> addToTracker)
        {
            var pastedCount = 0;
            foreach (var pair in _currentCopiedEdits.PropEdits)
            {
                var flags = pair.Value.OptionFlags;
                var member = Member.Find(target.GetType(), pair.Key, HasPropertyFlag(flags, PropertyTrackerData.PropertyTrackerDataOptions.IsProperty));
                if (member == null)
                    continue;

                // reset button and scene saving expect encoded values
                addToTracker(pair.Key, ValueConversion.Encode(member.TryGetValue(target), flags), flags);
                ValueConversion.SetValueWithFlags(member, target, pair.Value.Value, flags);
                pastedCount++;
            }
            return pastedCount;
        }

        internal struct CopiedEditValueTuple(object value, PropertyTrackerData.PropertyTrackerDataOptions flags)
        {
            /// <summary>
            /// Encoded value, see ValueConversion.
            /// </summary>
            public object Value = value;
            public PropertyTrackerData.PropertyTrackerDataOptions OptionFlags = flags;
        }

        internal class CopiedEdits(Dictionary<string, CopiedEditValueTuple> propEdits)
        {
            public Dictionary<string, CopiedEditValueTuple> PropEdits = propEdits;
        }
        #endregion copy / pasting edits
    }
}
