using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VirtualList;

namespace RSkoi_ComponentUtil.UI.VirtualList
{
    public class GenericListEntryView : MonoBehaviour, IViewFor<string>
    {
        public Button selfButton;
        public Text text;
        public Image bgImage;

        public void Set(string value)
        {
            text.text = value;
        }

        public void MarkAsEdited()
        {
            bgImage.color = Color.green;
        }

        public void ResetEdited()
        {
            bgImage.color = Color.white;
        }
    }

    public interface IViewFor<T>
    {
        void Set(T value);
    }

    public class GenericListEntrySource<TData, TView>(List<TData> list) : IListSource
        where TView : Component, IViewFor<TData>
    {
        private readonly List<TData> _list = list;

        public int Count
        {
            get
            {
                if (_list != null)
                    return _list.Count;
                else
                    return 0;
            }
        }

        public void SetItem(GameObject view, int index)
        {
            var element = _list[index];
            var display = view.GetComponent<TView>();
            display.Set(element);
        }
    }
}
