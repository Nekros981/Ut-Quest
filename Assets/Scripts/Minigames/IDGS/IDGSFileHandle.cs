using UnityEngine;
using UnityEngine.EventSystems;

namespace CampusQuest.IDGS
{
    public sealed class IDGSFileHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public IDGSFilesChallenge challenge;
        public int fileIndex;
        public void OnBeginDrag(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left)
                challenge.BeginDrag(fileIndex, data.position, data.pressEventCamera);
        }
        public void OnDrag(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left && challenge.DraggingIndex == fileIndex)
                challenge.MoveDrag(data.position, data.pressEventCamera);
        }
        public void OnEndDrag(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left && challenge.DraggingIndex == fileIndex)
                challenge.EndDrag(data.position, data.pressEventCamera);
        }
    }
}
