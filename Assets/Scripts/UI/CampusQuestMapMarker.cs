using System.Collections.Generic;
using UnityEngine;

public sealed class CampusQuestMapMarker : MonoBehaviour
{
    static readonly List<CampusQuestMapMarker> Active = new();

    public static IReadOnlyList<CampusQuestMapMarker> ActiveMarkers => Active;

    [SerializeField] Color dotColor = new Color(1f, 0.85f, 0.25f, 1f);

    public Color DotColor => dotColor;

    public Vector2 WorldPosition => transform.position;

    void OnEnable() => Active.Add(this);

    void OnDisable() => Active.Remove(this);
}
