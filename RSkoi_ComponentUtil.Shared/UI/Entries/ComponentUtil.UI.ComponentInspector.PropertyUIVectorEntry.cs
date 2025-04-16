using System;
using UnityEngine.UI;

namespace RSkoi_ComponentUtil.UI
{
    internal static partial class ComponentUtilUI
    {
        internal class PropertyUIVectorEntry(PropertyUIEntry entry)
        {
            private PropertyUIEntry _wrappedEntry = entry;
            private readonly InputField _input4 = entry.UiGO.transform.Find("Input4").GetComponent<InputField>(); // rightmost
            private readonly InputField _input3 = entry.UiGO.transform.Find("Input3").GetComponent<InputField>();
            private readonly InputField _input2 = entry.UiGO.transform.Find("Input2").GetComponent<InputField>();
            private readonly InputField _input1 = entry.UiGO.transform.Find("Input1").GetComponent<InputField>(); // leftmost

            /// <summary>
            /// event that fires when one input of the vector entry changes
            /// </summary>
            public Action<string> OnValueChanged;

            /// <summary>
            /// returns all four ui elements of this vector
            /// </summary>
            public InputField[] Inputs4
            {
                get
                {
                    return [_input1, _input2, _input3, _input4];
                }
            }
            /// <summary>
            /// returns three out of four ui elements of this vector
            /// </summary>
            public InputField[] Inputs3
            {
                get
                {
                    return [_input2, _input3, _input4];
                }
            }
            /// <summary>
            /// returns two out of four ui elements of this vector
            /// </summary>
            public InputField[] Inputs2
            {
                get
                {
                    return [_input3, _input4];
                }
            }

            /// <summary>
            /// returns encoded value string of all input fields
            /// </summary>
            /// <returns>values of input fields combined into string with # as separator</returns>
            public string GetUIVectorValuesAsString()
            {
                string value1 = _input1.gameObject.activeSelf ? _input1.text : "";
                string value2 = _input2.gameObject.activeSelf ? _input2.text : "";
                string value3 = _input3.gameObject.activeSelf ? _input3.text : "";
                string value4 = _input4.gameObject.activeSelf ? _input4.text : "";
                return $"{value1}#{value2}#{value3}#{value4}";
            }

            /// <summary>
            /// sets the values for valid input fields, configures visibility of input fields
            /// </summary>
            /// <param name="encValue"></param>
            public void SetUIVectorValues(string encValue)
            {
                InputField[] inputs = Inputs4;
                string[] split = encValue.Split('#');
                for (int i = 0; i < split.Length; i++)
                {
                    string s = split[i];
                    if (s == "")
                    {
                        inputs[i].gameObject.SetActive(false);
                        continue;
                    }

                    inputs[i].gameObject.SetActive(true);
                    InputField input = inputs[i];
                    if (input.gameObject.activeSelf)
                        input.text = s;
                }
            }

            /// <summary>
            /// sets the interactable property on each input field of vector ui entry
            /// </summary>
            /// <param name="b"></param>
            public void SetInteractable(bool b)
            {
                foreach (InputField i in Inputs4)
                    i.interactable = b;
            }

            /// <summary>
            /// sets the content type property on each input field of vector ui entry
            /// </summary>
            /// <param name="ct"></param>
            public void SetContentType(InputField.ContentType ct)
            {
                foreach (InputField i in Inputs4)
                    i.contentType = ct;
            }

            /// <summary>
            /// wires InputField.onValueChanged to PropertyUIVectorEntry.OnValueChanged
            /// </summary>
            public void RegisterInputEvents()
            {
                foreach (InputField i in Inputs4)
                    i.onValueChanged.AddListener(_ => OnValueChanged.Invoke(GetUIVectorValuesAsString()));
            }

            public void RemoveAllInputEvents()
            {
                foreach (InputField i in Inputs4)
                    i.onValueChanged.RemoveAllListeners();
            }
        }
    }
}
