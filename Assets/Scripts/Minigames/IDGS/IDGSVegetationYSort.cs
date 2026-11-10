using UnityEngine;
using UnityEngine.Rendering;

namespace CampusQuest.IDGS
{
    /// <summary>Opt-in foot-based sorting for IDGS prefabs only; no global renderer changes.</summary>
    [ExecuteAlways, DefaultExecutionOrder(1000), RequireComponent(typeof(SortingGroup))]
    public sealed class IDGSVegetationYSort : MonoBehaviour
    {
        public int baseOrder;
        private SortingGroup group;
        private void OnEnable() { Refresh(); }
        private void LateUpdate() { Refresh(); }
        public void Refresh()
        {
            if (group == null) group = GetComponent<SortingGroup>();
            group.sortingOrder = Mathf.Clamp(baseOrder - Mathf.RoundToInt(transform.position.y * 128), -30000, 30000);
        }
    }
}
