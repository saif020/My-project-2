using UnityEngine;

namespace ProcGen2D
{
    /// Answers per-tile questions for ANY tile, from the low-res Ooze map and noise.
    /// Everything depends only on (seed, tile coordinates), so every chunk agrees: no seams.
    public sealed class WorldBiomeSampler2D
    {
        readonly byte[,] map;
        readonly int lowRes;
        readonly float cellSize, amplitude, frequency;
        readonly float ox1, oy1, ox2, oy2;
        readonly float lakeFrequency, lakeThreshold;
        readonly float lx1, ly1, lx2, ly2;

        public WorldBiomeSampler2D(WorldSettings2D settings, byte[,] lowResMap)
        {
            map = lowResMap;
            lowRes = settings.LowResSize;
            cellSize = settings.LowResCellSizeInTiles;
            amplitude = settings.BorderWarpAmplitude;
            frequency = settings.BorderWarpFrequency;
            lakeFrequency = settings.LakeFrequency;
            lakeThreshold = settings.LakeThreshold;

            var rng = new System.Random(settings.Seed ^ 0x5bd1e995);
            ox1 = rng.Next(0, 10000); oy1 = rng.Next(0, 10000);
            ox2 = rng.Next(0, 10000); oy2 = rng.Next(0, 10000);
            lx1 = rng.Next(0, 10000); ly1 = rng.Next(0, 10000);
            lx2 = rng.Next(0, 10000); ly2 = rng.Next(0, 10000);
        }

        /// Ooze biome index at this tile (lakes NOT included).
        public byte SampleLand(int tileX, int tileY)
        {
            float wx = tileX + (Mathf.PerlinNoise(tileX * frequency + ox1, tileY * frequency + oy1) - 0.5f) * 2f * amplitude;
            float wy = tileY + (Mathf.PerlinNoise(tileX * frequency + ox2, tileY * frequency + oy2) - 0.5f) * 2f * amplitude;

            int cx = Mathf.Clamp(Mathf.FloorToInt(wx / cellSize), 0, lowRes - 1);
            int cy = Mathf.Clamp(Mathf.FloorToInt(wy / cellSize), 0, lowRes - 1);
            return map[cx, cy];
        }

        /// True where the raw lake noise says "water". The chunk painter still removes thin parts afterwards.
        public bool IsLakeRaw(int tileX, int tileY)
        {
            float f = lakeFrequency;
            float n = 0.65f * Mathf.PerlinNoise(tileX * f + lx1, tileY * f + ly1)
                    + 0.35f * Mathf.PerlinNoise(tileX * f * 2.3f + lx2, tileY * f * 2.3f + ly2);
            return n < lakeThreshold;
        }
    }
}