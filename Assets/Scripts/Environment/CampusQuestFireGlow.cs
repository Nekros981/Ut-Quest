using UnityEngine;
using UnityEngine.Rendering.Universal;

public sealed class CampusQuestFireGlow : MonoBehaviour
{
    const float BaseIntensity = 0.45f;
    const float IntensityAmp = 0.12f;
    const float BaseOuterRadius = 2.2f;
    const float RadiusAmp = 0.15f;

    [SerializeField] Light2D light2D;
    [SerializeField] float phase;

    void Awake()
    {
        if (light2D == null)
            light2D = GetComponent<Light2D>();
    }

    void Update()
    {
        if (light2D == null)
            return;

        float t = Time.time;
        float intensityNoise = Mathf.PerlinNoise(t * 3.5f + phase, 0f);
        light2D.intensity = BaseIntensity + (intensityNoise - 0.5f) * 2f * IntensityAmp;

        float radiusNoise = Mathf.PerlinNoise(0f, t * 2.2f + phase);
        light2D.pointLightOuterRadius = BaseOuterRadius + (radiusNoise - 0.5f) * 2f * RadiusAmp;
    }
}
