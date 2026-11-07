using UnityEngine;

namespace CampusQuest.Seguridad
{
    /// <summary>Animación de lluvia en pantalla (mosaico de gotas) para el bosque exterior de SP.</summary>
    public sealed class SpForestRainOverlay : MonoBehaviour
    {
        [SerializeField] Sprite[] frames;
        [SerializeField] float framesPerSecond = 10f;
        [SerializeField] SpriteRenderer[] tiles;

        int frameIndex;
        float timer;

        public void ConfigureGrid(Sprite[] sprites, SpriteRenderer[] renderers, float fps = 10f)
        {
            frames = sprites;
            tiles = renderers;
            framesPerSecond = fps;
            ApplyFrame(0);
        }

        void Awake()
        {
            if (tiles == null || tiles.Length == 0)
                tiles = GetComponentsInChildren<SpriteRenderer>(true);
            ApplyFrame(0);
        }

        void Update()
        {
            if (frames == null || frames.Length == 0 || tiles == null || tiles.Length == 0)
                return;

            timer += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(1f, framesPerSecond);
            while (timer >= frameDuration)
            {
                timer -= frameDuration;
                frameIndex = (frameIndex + 1) % frames.Length;
                ApplyFrame(frameIndex);
            }
        }

        void ApplyFrame(int index)
        {
            if (frames == null || frames.Length == 0)
                return;

            int baseFrame = frames.Length > 0 ? index % frames.Length : 0;
            for (int i = 0; i < tiles.Length; i++)
            {
                var tile = tiles[i];
                if (tile == null) continue;
                int offset = i % frames.Length;
                tile.sprite = frames[(baseFrame + offset) % frames.Length];
            }
        }
    }
}
