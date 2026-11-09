using UnityEngine;

namespace CampusQuest.Seguridad
{
    /// <summary>Ajusta el emisor de lluvia al tamaño de la cámara. El sistema va hijo de la cámara, así sigue a Mateo.</summary>
    public sealed class SpForestRainOverlay : MonoBehaviour
    {
        const float ViewMargin = 1.15f;
        const float RainPlaneZ = 10f;

        ParticleSystem particles;
        float lastOrthoSize = -1f;
        float lastAspect = -1f;

        void Awake()
        {
            particles = GetComponent<ParticleSystem>();
            EnsureParentedToFollowCamera();
            TuneExistingEmitter();
            FitEmitterToCamera();
        }

        void LateUpdate()
        {
            var cam = ResolveCamera();
            if (cam == null)
                return;

            if (Mathf.Approximately(cam.orthographicSize, lastOrthoSize) &&
                Mathf.Approximately(cam.aspect, lastAspect))
                return;

            FitEmitterToCamera();
        }

        public void FitEmitterToCamera()
        {
            if (particles == null)
                particles = GetComponent<ParticleSystem>();
            if (particles == null)
                return;

            var cam = ResolveCamera();
            if (cam == null || !cam.orthographic)
                return;

            lastOrthoSize = cam.orthographicSize;
            lastAspect = cam.aspect;

            float viewHeight = cam.orthographicSize * 2f * ViewMargin;
            float viewWidth = viewHeight * cam.aspect;

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(viewWidth, viewHeight, 0.1f);
        }

        void EnsureParentedToFollowCamera()
        {
            if (GetComponentInParent<Camera>() == null)
            {
                var cam = Camera.main;
                if (cam == null)
                    return;
                transform.SetParent(cam.transform, false);
            }

            transform.localPosition = new Vector3(0f, 0f, RainPlaneZ);
        }

        void TuneExistingEmitter()
        {
            if (particles == null)
                return;

            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
                renderer.maxParticleSize = 1f;

            var main = particles.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.1f);
            main.startColor = new Color(1f, 1f, 1f, 100f / 255f);
            main.maxParticles = 1000;

            var emission = particles.emission;
            emission.rateOverTime = 350f;
        }

        Camera ResolveCamera()
        {
            var cam = GetComponentInParent<Camera>();
            if (cam != null)
                return cam;
            return Camera.main;
        }
    }
}
