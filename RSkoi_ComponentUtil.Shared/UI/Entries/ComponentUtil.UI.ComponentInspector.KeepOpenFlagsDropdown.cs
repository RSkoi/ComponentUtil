using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RSkoi_ComponentUtil.UI
{
    internal static partial class ComponentUtilUI
    {
        internal class KeepOpenFlagsDropdown : MonoBehaviour, IPointerClickHandler, ISubmitHandler
        {
            const string DROPDOWN_LIST_NAME = "Dropdown List";

            /// <summary>
            /// Called with index when an option is clicked. Null for default behaviour.
            /// </summary>
            internal Action<int> OnOptionClicked;

            Dropdown _dropdown;
            Transform _openList;
            Text[] _openListLabels = [];

            void Awake() => _dropdown = GetComponent<Dropdown>();

            public void OnPointerClick(PointerEventData eventData)  => TakeOverOptions();
            public void OnSubmit(BaseEventData eventData)           => TakeOverOptions();

            void TakeOverOptions()
            {
                _openList = null;
                _openListLabels = [];
                if (OnOptionClicked == null)
                    return;

                _openList = transform.Find(DROPDOWN_LIST_NAME);
                if (_openList == null)
                    return;

                var toggles = _openList.GetComponentsInChildren<Toggle>(includeInactive: false);
                _openListLabels = new Text[toggles.Length];
                for (var i = 0; i < toggles.Length; i++)
                {
                    var toggle = toggles[i];
                    _openListLabels[i] = toggle.GetComponentInChildren<Text>();

                    if (toggle.graphic != null)
                    {
                        toggle.graphic.enabled = false;
                        toggle.graphic = null;
                    }

                    toggle.onValueChanged.RemoveAllListeners();
                    var optionIndex = i;
                    toggle.onValueChanged.AddListener(_ => OnOptionClicked?.Invoke(optionIndex));
                }
            }

            internal void RefreshOpenList()
            {
                if (_openList == null)
                    return;

                for (var i = 0; i < _openListLabels.Length && i < _dropdown.options.Count; i++)
                    if (_openListLabels[i] != null)
                        _openListLabels[i].text = _dropdown.options[i].text;
            }
        }
    }
}
