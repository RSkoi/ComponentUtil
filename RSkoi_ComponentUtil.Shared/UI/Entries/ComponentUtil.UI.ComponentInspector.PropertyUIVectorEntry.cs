using System;
using UnityEngine.UI;

namespace RSkoi_ComponentUtil.UI
{
    internal static partial class ComponentUtilUI
    {
        internal class PropertyUIVectorEntry(PropertyUIEntry entry)
        {
            readonly InputField _input4 = entry.UiGO.transform.Find("Input4").GetComponent<InputField>(); // rightmost
            readonly InputField _input3 = entry.UiGO.transform.Find("Input3").GetComponent<InputField>();
            readonly InputField _input2 = entry.UiGO.transform.Find("Input2").GetComponent<InputField>();
            readonly InputField _input1 = entry.UiGO.transform.Find("Input1").GetComponent<InputField>(); // leftmost

            /// <summary>
            /// Event that fires when one input of the vector entry changes.
            /// </summary>
            public Action<string> OnValueChanged;

            /// <summary>
            /// Returns all four ui elements of this vector.
            /// </summary>
            public InputField[] Inputs4
            {
                get => [_input1, _input2, _input3, _input4];
            }

            /// <summary>
            /// Returns encoded value string of all input fields.
            /// </summary>
            /// <returns>values of input fields combined into string with # as separator</returns>
            public string GetUIVectorValuesAsString()
            {
                var value1 = _input1.gameObject.activeSelf ? _input1.text : "";
                var value2 = _input2.gameObject.activeSelf ? _input2.text : "";
                var value3 = _input3.gameObject.activeSelf ? _input3.text : "";
                var value4 = _input4.gameObject.activeSelf ? _input4.text : "";
                return $"{value1}#{value2}#{value3}#{value4}";
            }

            /// <summary>
            /// Sets the values for valid input fields, configures visibility of input fields.
            /// </summary>
            /// <param name="encValue"></param>
            public void SetUIVectorValues(string encValue)
            {
                var inputs = Inputs4;
                var split = encValue.Split('#');
                for (var i = 0; i < split.Length; i++)
                {
                    var s = split[i];
                    if (s == "")
                    {
                        inputs[i].gameObject.SetActive(false);
                        continue;
                    }

                    inputs[i].gameObject.SetActive(true);
                    var input = inputs[i];
                    if (input.gameObject.activeSelf)
                        input.text = s;
                }
            }

            /// <summary>
            /// Sets the interactable property on each input field of vector ui entry.
            /// </summary>
            /// <param name="b"></param>
            public void SetInteractable(bool b)
            {
                foreach (var i in Inputs4)
                    i.interactable = b;
            }

            /// <summary>
            /// Wires InputField.onValueChanged to PropertyUIVectorEntry.OnValueChanged.
            /// </summary>
            public void RegisterInputEvents()
            {
                foreach (var i in Inputs4)
                    i.onValueChanged.AddListener(_ => OnValueChanged.Invoke(GetUIVectorValuesAsString()));
            }

            public void RemoveAllInputEvents()
            {
                foreach (var i in Inputs4)
                    i.onValueChanged.RemoveAllListeners();
            }
        }
    }
}
