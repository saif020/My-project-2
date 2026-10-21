using UnityEngine;

namespace ProcGen2D
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class BiomeMapDebugView2D : MonoBehaviour
    {
        public WorldSettings2D Settings;
        [Tooltip("Width of the preview in Unity units (the map is scaled to fit).")]
        public float DisplaySizeInUnits = 100f;
        [Tooltip("Regenerate automatically when any setting changes in Play mode.")]
        public bool AutoRegenerate = true;

        Texture2D texture;
        Sprite sprite;
        int lastSignature;

        void Start() => Generate();

        void Update()
        {
            if (!AutoRegenerate || Settings == null) return;
            if (ComputeSignature() != lastSignature) Generate();
        }

        void OnDestroy()
        {
            if (sprite != null) Destroy(sprite);
            if (texture != null) Destroy(texture);
        }

        // Changes whenever any value that affects the map (or its display) changes.
        int ComputeSignature()
        {
            var h = new System.HashCode();
            h.Add(DisplaySizeInUnits);
            h.Add(Settings.Seed);
            h.Add(Settings.WorldSizeInTiles);
            h.Add(Settings.LowResCellSizeInTiles);
            h.Add(Settings.SeedPointDensity);
            h.Add(Settings.Biomes.Count);
            foreach (var b in Settings.Biomes)
            {
                if (b == null) { h.Add(0); continue; }
                h.Add(b.DebugColor);
                h.Add(b.SelectionWeight);
                h.Add(b.MinIntensity);
                h.Add(b.MaxIntensity);
                h.Add(b.MinDecayRate);
                h.Add(b.MaxDecayRate);
            }
            return h.ToHashCode();
        }

        void Generate()
        {
            if (Settings == null) return;
            lastSignature = ComputeSignature(); // set first so errors below don't spam every frame

            if (Settings.Biomes.Count == 0 || Settings.Biomes.Contains(null))
            {
                Debug.LogError("BiomeMapDebugView2D: WorldSettings2D needs at least one biome and no empty slots.");
                return;
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            byte[,] map = OozeBiomeMap2D.Generate(Settings, out _);
            sw.Stop();

            int size = Settings.LowResSize;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = Settings.Biomes[map[x, y]].DebugColor;

            if (texture == null || texture.width != size)
            {
                if (texture != null) Destroy(texture);
                texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
            }
            texture.SetPixels32(pixels);
            texture.Apply();

            if (sprite != null) Destroy(sprite);
            sprite = Sprite.Create(texture, new Rect(0, 0, size, size),
                                   new Vector2(0.5f, 0.5f), size / DisplaySizeInUnits);
            GetComponent<SpriteRenderer>().sprite = sprite;

            Debug.Log($"Ooze biome map {size}x{size} generated in {sw.ElapsedMilliseconds} ms");
        }
    }
}