using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class CampusQuestMinimapFollow : MonoBehaviour
{
    const float Depth = -10f;

    [SerializeField] Transform followTarget;
    Camera minimapCamera;

    void Awake()
    {
        minimapCamera = GetComponent<Camera>();
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = 9f;
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = new Color(0.05f, 0.08f, 0.06f, 1f);
        minimapCamera.cullingMask &= ~(1 << 5);
    }

    void Start()
    {
        if (followTarget == null)
        {
            var mateo = FindAnyObjectByType<CampusQuestMateoPlayerInput>();
            if (mateo != null)
                followTarget = mateo.transform;
        }
    }

    void LateUpdate()
    {
        if (followTarget == null)
            return;

        var p = followTarget.position;
        transform.position = new Vector3(p.x, p.y, Depth);
    }

    public void SetFollowTarget(Transform target) => followTarget = target;
}
