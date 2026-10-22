using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using RSkoi_ComponentUtil.UI;
using RSkoi_ComponentUtil.Core;

namespace RSkoi_ComponentUtil
{
    public partial class ComponentUtil
    {
        /// <summary>
        /// Sets up component inspector: configure delete component button,
        /// get all fields of currently selected component, map to list entries of different types.
        /// </summary>
        /// <param name="input">selected component to list the properties and fields of</param>
        /// <param name="inputUi">selected component ui entry</param>
        internal void GetAllFieldsAndProperties(Component input, ComponentUtilUI.GenericUIListEntry inputUi)
        {
            ComponentUtilUI._componentDeleteButton.interactable = false;
            if (input == null || inputUi == null)
                return;

            #region component delete button
            ComponentUtilUI._componentDeleteButton.onClick.RemoveAllListeners();
            if (ComponentIsTracked(_selectedObject, input.gameObject, input.GetType().FullName))
            {
                ComponentUtilUI._componentDeleteButton.interactable = true;
                ComponentUtilUI._componentDeleteButton.onClick.AddListener(() =>
                {
                    var go = input.gameObject;
                    if (RemoveComponentFromTracker(_selectedObject, go, input))
                        DestroyImmediate(input); // scary, so that GetOrCacheComponents doesn't pick it up again

                    // force refresh cache
                    ComponentUtilCache.ClearComponentFromCache(input);
                    ComponentUtilCache.GetOrCacheComponents(go, forceRefresh: true);

                    Entry(_selectedObject);
                });
            }
            #endregion component delete button

            ComponentUtilUI.ResetAndDisablePropertyEntries();

            ComponentUtilUI.UpdateUISelectedText(
                ComponentUtilUI._componentPropertyListSelectedComponentText,
                $"{input.gameObject.name}.{input.GetType().Name}");
            ComponentUtilUI.UpdateUISelectedText(
                ComponentUtilUI._objectPropertyListSelectedText,
                "None");
            _selectedReferencePropertyUiEntry = null;

            // otherwise the color picker from a color prop can produce a null ref
            if (Studio.Studio.Instance.colorPalette.visible)
                Studio.Studio.Instance.colorPalette.visible = false;

            // otherwise changing value in ObjectInspector will fail with exception
            if (ComponentUtilUI._objectInspectorWindow.gameObject.activeSelf)
                ComponentUtilUI._objectInspectorWindow.gameObject.SetActive(false);

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

                ConfigurePropertyEntry(_selectedComponentUiEntry, input, Member.Of(p));
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

                ConfigurePropertyEntry(_selectedComponentUiEntry, input, Member.Of(f));
            }
        }

        /// <param name="parentUiEntry">selected component entry</param>
        /// <param name="input">selected Component (ComponentInspector), or a referenced object (ObjectInspector)</param>
        /// <param name="member">property / field of input to create an entry for</param>
        /// <param name="objectMode">whether we're populating the ObjectInspector (true) or ComponentInspector (false)</param>
        void ConfigurePropertyEntry(ComponentUtilUI.GenericUIListEntry parentUiEntry, object input, Member member, bool objectMode = false)
        {
            if (input == null)
            {
                _logger.LogError($"ConfigurePropertyEntry: received null input for {member}");
                return;
            }

            // input cast to Component in the ComponentInspector
            var cInput = objectMode ? null : (Component)input;
            if (!objectMode && cInput == null)
            {
                _logger.LogError("ConfigurePropertyEntry: objectMode is false, but could not convert input to Component");
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

            var ctx = new PropertyEntryContext(parentUiEntry, uiEntry, input, member, value, kind.ToTrackerFlag() | member.MemberFlag, objectMode);
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
                        _logger.LogWarning($"Could not configure vector property entry on {owner} with name {member.Name}, skipping");
                        // entry is pooled and inactive, leave it instead of destroying
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
                var key = new PropertyKey(_selectedObject, cInput.gameObject, cInput);
                if (PropertyIsTracked(key, member.Name))
                    uiEntry.SetBgColorEdited();
            }
            else if (objectMode) // property inside reference type / the object inspector
            {
                var c = (Component)parentUiEntry.UiTarget;
                var key = new PropertyReferenceKey(_selectedObject, c.gameObject, c, _selectedReferencePropertyUiEntry.MemberName);
                // make bg green if value is edited
                if (PropertyIsTracked(key, member.Name))
                    MarkAsEditedIfChanged(ctx, GetTrackedDefaultValue(key, member.Name));

                ConfigResetReferenceProperty(key, ctx);
                ConfigTimelineReferenceProperty(key, ctx);
            }
            else // property in the component inspector
            {
                var key = new PropertyKey(_selectedObject, cInput.gameObject, cInput);
                // make bg green if value is edited
                if (PropertyIsTracked(key, member.Name))
                    MarkAsEditedIfChanged(ctx, GetTrackedDefaultValue(key, member.Name));

                ConfigReset(key, ctx);
                ConfigTimeline(key, ctx);
            }

            uiEntry.UiGO.SetActive(true);
        }

        void SetupNullEntry(Member member, bool objectMode)
        {
            var uiNullEntry = ComponentUtilUI.GetOrInstantiatePropEntryFromPool(ComponentUtilUI.NullEntryPrefab, objectMode);

            uiNullEntry.PropertyName.text = member.Label;
            uiNullEntry.MemberName = member.Name;
            uiNullEntry.ResetButton.interactable = false;
            uiNullEntry.TimelineButton.interactable = false;

            uiNullEntry.UiGO.SetActive(true);
        }

        /// <param name="defaultValue">encoded default value of the member (see ValueConversion)</param>
        void MarkAsEditedIfChanged(PropertyEntryContext ctx, object defaultValue)
        {
            if (defaultValue == null)
            {
                _logger.LogError($"MarkAsEditedIfChanged: no defaultValue for property {ctx.Member.Name} found or value is null");
                return;
            }

            // compare encoded versions, current value is not encoded yet
            if (InvariantConversion.ToInvariantString(defaultValue) != ValueConversion.EncodeToString(ctx.Value, ctx.Flags))
                ctx.UiEntry.SetBgColorEdited();
        }

        InputField.ContentType MapTypeToInputContentType(Type type)
        {
            if (type.Equals(typeof(string)))
                return InputField.ContentType.Standard;
            else if (InvariantConversion.IsFloatingPoint(type))
                return InputField.ContentType.DecimalNumber;
            else
                return InputField.ContentType.IntegerNumber; // trying to cram an integer into e.g. a short will lead to problems
        }

        #region internal helpers
        internal static bool HasPropertyFlag(
            PropertyTrackerData.PropertyTrackerDataOptions input,
            PropertyTrackerData.PropertyTrackerDataOptions flagToCheck)
            => (input & flagToCheck) == flagToCheck;

        internal static void SetValueWithFlags(
            Member member,
            object targetObject,
            object value,
            PropertyTrackerData.PropertyTrackerDataOptions flags)
        {
            if (member == null)
            {
                _logger.LogError("SetValueWithFlags was passed no member");
                return;
            }
            if (targetObject == null)
            {
                _logger.LogError("SetValueWithFlags was passed a null targetObject");
                return;
            }
            // this shit is beyond cursed
            // this is probably a destroyed Unity object that's not actually been set to null yet
            if (targetObject.ToString() == "null")
                return;

            try
            {
                member.SetValue(targetObject, ValueConversion.Decode(value, member.MemberType, flags));
            }
            catch (Exception e)
            {
                _logger.LogError(e);
                _logger.LogError($"Tried to set value {value} on {targetObject}.{member.Name}");
            }
        }
        #endregion internal helpers

        #region pages
        internal void OnFilterComponentInspector()
        {
            if (_selectedComponent == null)
                return;

            GetAllFieldsAndProperties(_selectedComponent, _selectedComponentUiEntry);

            ComponentUtilUI.TraverseAndSetEditedParents();
        }

        internal void OnRefreshComponentInspector()
        {
            if (_selectedComponent == null)
                return;

            ComponentUtilCache.GetOrCachePropertyInfos(_selectedComponent, forceRefresh: true);
            ComponentUtilCache.GetOrCacheFieldInfos(_selectedComponent, forceRefresh: true);
            GetAllFieldsAndProperties(_selectedComponent, _selectedComponentUiEntry);

            _logger.LogMessage("Force refreshed ComponentInspector cache");

            ComponentUtilUI.TraverseAndSetEditedParents();
        }
        #endregion pages

        #region copy / pasting edits
        internal void OnCopyEdits(bool objectMode = false)
        {
            Dictionary<string, PropertyTrackerData> propEdits;
            object source;
            string sourceName;
            if (!objectMode)
            {
                var key = new PropertyKey(_selectedObject, _selectedGO, _selectedComponent);
                _propertyTracker.TryGetValue(key, out propEdits);
                source = _selectedComponent;
                sourceName = $"{_selectedGO.name}.{_selectedComponent.GetType().Name}";
            }
            else
            {
                var refKey = new PropertyReferenceKey(_selectedObject, _selectedGO, _selectedComponent, _selectedReferencePropertyUiEntry.MemberName);
                _referencePropertyTracker.TryGetValue(refKey, out propEdits);
                source = _selectedReferencePropertyUiEntry.Wrapper;
                sourceName = $"{_selectedGO.name}.{_selectedComponent.GetType().Name}.{_selectedReferencePropertyUiEntry.MemberName}";
            }

            if (propEdits == null)
            {
                _logger.LogMessage("No property or field edits to copy");
                return;
            }

            _currentCopiedEdits = new(GetEncodedValuesOfObj(source, propEdits));

            ComponentUtilUI._componentPropertyPasteButton.interactable = true;
            ComponentUtilUI._objectPropertyPasteButton.interactable = true;

            _logger.LogMessage($"Copied property and field edits of {sourceName}");
        }

        /// <returns>current values of the tracked members of obj, encoded (see ValueConversion)</returns>
        Dictionary<string, CopiedEditValueTuple> GetEncodedValuesOfObj(object obj, Dictionary<string, PropertyTrackerData> trackerData)
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

        internal void OnPasteEdits(bool objectMode = false)
        {
            if (_currentCopiedEdits == null)
            {
                ComponentUtilUI._componentPropertyPasteButton.interactable = false;
                ComponentUtilUI._objectPropertyPasteButton.interactable = false;
                return;
            }

            if (objectMode)
            {
                var referencePropertyName = _selectedReferencePropertyUiEntry.MemberName;
                PasteEdits(_selectedReferencePropertyUiEntry.Wrapper, (propName, defaultValue, flags)
                    => AddPropertyToTracker(_selectedObject, _selectedGO, _selectedComponent, referencePropertyName, propName, defaultValue, flags)
                );
            }
            else
            {
                var key = new PropertyKey(_selectedObject, _selectedGO, _selectedComponent);
                PasteEdits(_selectedComponent, (propName, defaultValue, flags) => AddPropertyToTracker(key, propName, defaultValue, flags));
            }

            OnRefreshComponentInspector();
        }

        /// <param name="target">the component or referenced object to paste the edits onto</param>
        /// <param name="addToTracker">action to add an edit to the tracker: member name, encoded default value, flags</param>
        void PasteEdits(object target, Action<string, object, PropertyTrackerData.PropertyTrackerDataOptions> addToTracker)
        {
            foreach (var pair in _currentCopiedEdits.PropEdits)
            {
                var flags = pair.Value.OptionFlags;
                var member = Member.Find(target.GetType(), pair.Key, HasPropertyFlag(flags, PropertyTrackerData.PropertyTrackerDataOptions.IsProperty));
                if (member == null)
                    continue;

                // reset button and scene saving expect encoded values
                addToTracker(pair.Key, ValueConversion.Encode(member.TryGetValue(target), flags), flags);
                SetValueWithFlags(member, target, pair.Value.Value, flags);
            }
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
