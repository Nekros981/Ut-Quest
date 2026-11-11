using UnityEngine;
using UnityEngine.EventSystems;

namespace CampusQuest.IDGS
{
    public sealed class IDGSCableHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public IDGSCablesChallenge challenge;
        public int cableIndex;
        public void OnBeginDrag(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left)
                challenge.BeginDrag(cableIndex, data.position, data.pressEventCamera);
        }
        public void OnDrag(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left && challenge.DraggingIndex == cableIndex)
                challenge.MoveDrag(data.position, data.pressEventCamera);
        }
        public void OnEndDrag(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left && challenge.DraggingIndex == cableIndex)
                challenge.EndDrag(data.position, data.pressEventCamera);
        }
    }
}
