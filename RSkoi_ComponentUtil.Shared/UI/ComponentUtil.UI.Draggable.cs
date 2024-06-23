using UnityEngine;
using UnityEngine.EventSystems;

namespace RSkoi_ComponentUtil.UI
{
    public class ComponentUtilDraggable : MonoBehaviour, IDragHandler
    {
        public RectTransform target;

        public void OnDrag(PointerEventData eventData)
        {
            target.position += new Vector3(eventData.delta.x, eventData.delta.y);
        }
    }
}
