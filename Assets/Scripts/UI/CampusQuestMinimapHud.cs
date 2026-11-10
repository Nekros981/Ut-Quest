using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class CampusQuestMinimapHud : MonoBehaviour
{
    const float ViewWorldSize = 18f;
    const int MaxDots = 12;

    [SerializeField] RawImage mapImage;
    [SerializeField] RectTransform markersRoot;
    [SerializeField] float mapPixels = 220f;

    Transform player;
    readonly List<Image> dots = new();

    void Start()
    {
        var mateo = FindAnyObjectByType<CampusQuestMateoPlayerInput>();
        if (mateo != null)
            player = mateo.transform;

        if (markersRoot == null || dots.Count > 0)
            return;

        for (int i = 0; i < MaxDots; i++)
        {
            var go = new GameObject("MapDot_" + i, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(markersRoot, false);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.color = Color.white;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(10f, 10f);
            rt.anchoredPosition = Vector2.zero;
            go.SetActive(false);
            dots.Add(img);
        }
    }

    void LateUpdate()
    {
        if (player == null)
        {
            var mateo = FindAnyObjectByType<CampusQuestMateoPlayerInput>();
            if (mateo != null)
                player = mateo.transform;
        }

        if (player == null || markersRoot == null)
            return;

        var markers = CampusQuestMapMarker.ActiveMarkers;
        float halfView = ViewWorldSize * 0.5f;
        float halfMap = mapPixels * 0.5f;
        float edgeScale = halfMap * 0.92f;

        for (int i = 0; i < dots.Count; i++)
        {
            if (i >= markers.Count)
            {
                dots[i].gameObject.SetActive(false);
                continue;
            }

            var marker = markers[i];
            if (marker == null || !marker.isActiveAndEnabled)
            {
                dots[i].gameObject.SetActive(false);
                continue;
            }

            Vector2 delta = marker.WorldPosition - (Vector2)player.position;
            Vector2 norm = new Vector2(delta.x / halfView, delta.y / halfView);
            float mag = norm.magnitude;
            if (mag > 1f)
                norm /= mag;

            dots[i].color = marker.DotColor;
            dots[i].rectTransform.anchoredPosition = norm * edgeScale;
            dots[i].gameObject.SetActive(true);
        }
    }
}
