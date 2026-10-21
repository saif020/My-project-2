using System.Collections.Generic;
using UnityEngine;

namespace ProcGen2D
{
    /// Ooze biome generation on a low-resolution grid.
    /// Same algorithm as BiomeGenerator_OozeBased (GameDevEducation), but:
    ///  - uses a visited "stamp" instead of allocating bool[,]/float[,] per seed (needed at 500x500)
    ///  - fills cells the ooze never reached with the nearest biome
    public static class OozeBiomeMap2D
    {
        struct Node { public int X, Y; public float Intensity; }

        static readonly int[] OffX = { 0, 0, 1, -1, 1, -1, 1, -1 };
        static readonly int[] OffY = { 1, -1, 0, 0, 1, -1, -1, 1 };
        static readonly float[] OffLen = { 1f, 1f, 1f, 1f, 1.41421356f, 1.41421356f, 1.41421356f, 1.41421356f };

        public static byte[,] Generate(WorldSettings2D settings, out float[,] strengths)
        {
            int size = settings.LowResSize;
            var map = new byte[size, size];
            strengths = new float[size, size];
            var assigned = new bool[size, size];
            var stamp = new int[size, size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    stamp[x, y] = -1;

            var rng = new System.Random(settings.Seed);
            int seedCount = Mathf.Max(1, Mathf.FloorToInt(size * size * settings.SeedPointDensity));
            var queue = new Queue<Node>();

            for (int s = 0; s < seedCount; s++)
            {
                int sx = rng.Next(size);
                int sy = rng.Next(size);
                byte biomeIndex = PickBiome(settings, rng);
                BiomeData2D biome = settings.Biomes[biomeIndex];

                float start = Mathf.Lerp(biome.MinIntensity, biome.MaxIntensity, (float)rng.NextDouble());
                queue.Clear();
                queue.Enqueue(new Node { X = sx, Y = sy, Intensity = start });

                while (queue.Count > 0)
                {
                    Node n = queue.Dequeue();
                    map[n.X, n.Y] = biomeIndex;
                    strengths[n.X, n.Y] = n.Intensity;
                    assigned[n.X, n.Y] = true;
                    stamp[n.X, n.Y] = s;

                    for (int i = 0; i < 8; i++)
                    {
                        int nx = n.X + OffX[i];
                        int ny = n.Y + OffY[i];
                        if (nx < 0 || ny < 0 || nx >= size || ny >= size) continue;
                        if (stamp[nx, ny] == s) continue;
                        stamp[nx, ny] = s;

                        float decay = Mathf.Lerp(biome.MinDecayRate, biome.MaxDecayRate, (float)rng.NextDouble()) * OffLen[i];
                        float next = n.Intensity - decay;
                        if (next <= 0f) continue;
                        queue.Enqueue(new Node { X = nx, Y = ny, Intensity = next });
                    }
                }
            }

            FillGaps(map, assigned, size);
            return map;
        }

        static byte PickBiome(WorldSettings2D settings, System.Random rng)
        {
            float total = 0f;
            foreach (var b in settings.Biomes) total += b.SelectionWeight;
            float roll = (float)rng.NextDouble() * total;
            for (int i = 0; i < settings.Biomes.Count; i++)
            {
                roll -= settings.Biomes[i].SelectionWeight;
                if (roll <= 0f) return (byte)i;
            }
            return (byte)(settings.Biomes.Count - 1);
        }

        // Multi-source BFS: every unassigned cell takes the biome of the nearest assigned cell.
        static void FillGaps(byte[,] map, bool[,] assigned, int size)
        {
            var queue = new Queue<Vector2Int>();
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    if (assigned[x, y]) queue.Enqueue(new Vector2Int(x, y));

            while (queue.Count > 0)
            {
                Vector2Int p = queue.Dequeue();
                for (int i = 0; i < 4; i++)
                {
                    int nx = p.x + OffX[i];
                    int ny = p.y + OffY[i];
                    if (nx < 0 || ny < 0 || nx >= size || ny >= size || assigned[nx, ny]) continue;
                    assigned[nx, ny] = true;
                    map[nx, ny] = map[p.x, p.y];
                    queue.Enqueue(new Vector2Int(nx, ny));
                }
            }
        }
    }
}