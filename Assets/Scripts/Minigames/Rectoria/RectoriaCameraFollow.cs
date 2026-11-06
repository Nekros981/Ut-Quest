using UnityEngine;

namespace CampusQuest.Rectoria
{
    public sealed class RectoriaCameraFollow : MonoBehaviour
    {
        public Transform target;
        public float smoothTime = 0.12f;
        private Vector3 velocity;
        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        }
    }
}
