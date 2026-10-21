using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProcGen2D
{
    /// Step 4 test: paints a square of chunks around a chosen chunk and repaints when settings change.
    /// (Step 5 replaces this with real loading/unloading of chunks around the player.)
    public class WorldPreview2D : MonoBehaviour
    {
        public WorldSettings2D Settings;
        public Tilemap BaseMap;
        public Tilemap OverlayMap;

        [Tooltip("Paints (2*radius+1)^2 chunks.")]
        [Range(0, 6)] public int ChunkRadius = 2;
        public bool UseWorldCenter = true;
        public Vector2Int CenterChunk;
        public bool MoveCamera = true;

        int lastSignature;
        bool painted;

        void Update()
        {
            if (Settings == null || BaseMap == null || OverlayMap == null) return;
            int sig = ComputeSignature();
            if (painted && sig == lastSignature) return;
            painted = true;
            lastSignature = sig;
            Repaint();
        }

        int ComputeSignature()
        {
            var h = new System.HashCode();
            h.Add(ChunkRadius); h.Add(UseWorldCenter); h.Add(CenterChunk); h.Add(MoveCamera);
            h.Add(Settings.Seed); h.Add(Settings.WorldSizeInTiles); h.Add(Settings.ChunkSizeInTiles);
            h.Add(Settings.LowResCellSizeInTiles); h.Add(Settings.SeedPointDensity);
            h.Add(Settings.BorderWarpAmplitude); h.Add(Settings.BorderWarpFrequency);
            h.Add(Settings.LakeFrequency); h.Add(Settings.LakeThreshold);
            h.Add(Settings.WaterBiome);
            if (Settings.WaterBiome != null) { h.Add(Settings.WaterBiome.BaseTile); h.Add(Settings.WaterBiome.OverlaySet); }
            h.Add(Settings.Biomes.Count);
            foreach (var b in Settings.Biomes)
            {
                if (b == null) { h.Add(0); continue; }
                h.Add(b.SelectionWeight); h.Add(b.MinIntensity); h.Add(b.MaxIntensity);
                h.Add(b.MinDecayRate); h.Add(b.MaxDecayRate);
                h.Add(b.BaseTile); h.Add(b.OverlaySet);
            }
            return h.ToHashCode();
        }

        void Repaint()
        {
            if (Settings.Biomes.Count == 0 || Settings.Biomes.Contains(null))
            {
                Debug.LogError("WorldPreview2D: WorldSettings2D needs biomes with no empty slots.");
                return;
            }
            foreach (var b in Settings.Biomes)
                if (b.BaseTile == null)
                {
                    Debug.LogError($"WorldPreview2D: biome '{b.name}' has no Base Tile.");
                    return;
                }

            if (Settings.WaterBiome != null)
            {
                if (Settings.WaterBiome.BaseTile == null)
                {
                    Debug.LogError("WorldPreview2D: the Water Biome has no Base Tile.");
                    return;
                }
                if (Settings.Biomes.Contains(Settings.WaterBiome))
                    Debug.LogWarning("WorldPreview2D: the Water Biome is also in the Biomes list. Remove it from the list; lakes come from noise now.");
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();

            byte[,] map = OozeBiomeMap2D.Generate(Settings, out _);
            var sampler = new WorldBiomeSampler2D(Settings, map);

            int perSide = Settings.ChunksPerSide;
            Vector2Int center = UseWorldCenter ? new Vector2Int(perSide / 2, perSide / 2) : CenterChunk;

            BaseMap.ClearAllTiles();
            OverlayMap.ClearAllTiles();

            int paintedChunks = 0;
            for (int cx = center.x - ChunkRadius; cx <= center.x + ChunkRadius; cx++)
                for (int cy = center.y - ChunkRadius; cy <= center.y + ChunkRadius; cy++)
                {
                    if (cx < 0 || cy < 0 || cx >= perSide || cy >= perSide) continue; // outside the world
                    ChunkPainter2D.Paint(Settings, sampler, BaseMap, OverlayMap, new Vector2Int(cx, cy));
                    paintedChunks++;
                }

            if (MoveCamera && Camera.main != null)
            {
                float half = Settings.ChunkSizeInTiles * 0.5f;
                Vector3 p = Camera.main.transform.position;
                Camera.main.transform.position = new Vector3(
                    (center.x * Settings.ChunkSizeInTiles) + half,
                    (center.y * Settings.ChunkSizeInTiles) + half, p.z);
            }

            sw.Stop();
            Debug.Log($"WorldPreview2D: {paintedChunks} chunks painted in {sw.ElapsedMilliseconds} ms");
        }
    }
}