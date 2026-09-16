using UnityEngine.EventSystems;

namespace MazeZero
{
    public sealed class CameraDragZone : UIBehaviour, IDragHandler
    {
        public void OnDrag(PointerEventData eventData) => OnScreenControls.AddCameraDelta(eventData.delta);
    }
}
