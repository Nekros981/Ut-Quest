using UnityEngine;

namespace CampusQuest.IDGS
{
    [RequireComponent(typeof(Camera))]
    public sealed class IDGSExteriorCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Rect mapBounds = new Rect(0, 0, 64, 52);
        private Camera view;

        private void Awake() => view = GetComponent<Camera>();

        private void LateUpdate()
        {
            if (target == null) return;

            float halfHeight = view.orthographicSize;
            float halfWidth = halfHeight * view.aspect;
            Vector3 position = transform.position;
            position.x = ClampAxis(target.position.x, mapBounds.xMin, mapBounds.xMax, halfWidth);
            position.y = ClampAxis(target.position.y, mapBounds.yMin, mapBounds.yMax, halfHeight);
            transform.position = position;
        }

        private static float ClampAxis(float position, float min, float max, float halfView)
        {
            return max - min <= 2 * halfView
                ? (min + max) * .5f
                : Mathf.Clamp(position, min + halfView, max - halfView);
        }
    }
}
