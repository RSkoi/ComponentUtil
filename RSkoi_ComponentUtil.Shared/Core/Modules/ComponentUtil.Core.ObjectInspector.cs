using System;
using System.Reflection;
using UnityEngine;

using RSkoi_ComponentUtil.UI;
using RSkoi_ComponentUtil.Core;

namespace RSkoi_ComponentUtil
{
    public partial class ComponentUtil
    {
        internal void OpenObjectInspector(PropertyInfo p, FieldInfo f, Type type, object value,
            ComponentUtilUI.PropertyUIEntry uiEntry, ComponentUtilUI.GenericUIListEntry parentUiEntry)
        {
            bool isProperty = p != null;
            bool isField = f != null;
            // this should never be the case
            if ((!isProperty && !isField) || (isProperty && isField))
            {
                _logger.LogError("OpenObjectInspector: pass either PropertyInfo (X)OR FieldInfo as arguments");
                return;
            }

            _selectedReferencePropertyUiEntry = uiEntry;

            if (!ComponentUtilUI._objectInspectorWindow.gameObject.activeSelf)
                ComponentUtilUI._objectInspectorWindow.gameObject.SetActive(true);

            string propName = isProperty ? p.Name : f.Name;
            GetAllFieldsAndPropertiesOfObject(propName, type, value, parentUiEntry);
        }

        /// <summary>
        /// sets up object inspector: gets all fields of given object, maps to list entries of different types
        /// </summary>
        /// <param name="input">referenced object to list the properties and fields of</param>
        /// <param name="inputUi">selected component ui entry</param>
        private void GetAllFieldsAndPropertiesOfObject(string propName, Type type, object input, ComponentUtilUI.GenericUIListEntry inputUi)
        {
            if (input == null || inputUi == null)
                return;

            ComponentUtilUI.ClearEntryListGO(ComponentUtilUI._objectPropertyListEntries);
            ComponentUtilUI.ClearEntryListGO(ComponentUtilUI._objectFieldListEntries);

            Component cParent = (Component)inputUi.UiTarget;
            ComponentUtilUI.UpdateUISelectedText(
                ComponentUtilUI._objectPropertyListSelectedText,
                $"{cParent.gameObject.name}.{cParent.GetType().Name}.{propName} ({type.Name})");

            foreach (PropertyInfo p in ComponentUtilCache.GetOrCachePropertyInfosObject(input))
            {
                // TODO: strings

                // no recursing in the object inspector else it could lead to circular hell
                if (!p.PropertyType.IsValueType)
                    continue;

                // ignore properties with private getters
                if (p.GetGetMethod() == null)
                    continue;

                ConfigurePropertyEntry(_selectedComponentUiEntry, input, p, null, true);
            }

            foreach (FieldInfo f in ComponentUtilCache.GetOrCacheFieldInfosObject(input))
            {
                // no recursing in the object inspector else it could lead to circular hell
                if (!f.FieldType.IsValueType)
                    continue;

                ConfigurePropertyEntry(_selectedComponentUiEntry, input, null, f, true);
            }
        }
    }
}
