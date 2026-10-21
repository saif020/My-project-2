using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProcGen2D
{
    public static class ChunkPainter2D
    {
        /// Lakes thinner than this (in tiles) are removed.
        public const int LakeMinWidth = 3;

        // Every cleanup/autotile step looks at neighbours, so each chunk is evaluated with extra tiles
        // around it:  1 (autotile neighbour) + 1 (2x2 overlay cleanup) + (LakeMinWidth - 1) (lake cleanup).
        // With this margin a chunk gets exactly the same result as painting the whole world at once.
        public const int Margin = 2 + (LakeMinWidth - 1);

        public static void Paint(WorldSettings2D settings, WorldBiomeSampler2D sampler,
                                 Tilemap baseMap, Tilemap overlayMap, Vector2Int chunk)
        {
            int cs = settings.ChunkSizeInTiles;
            int x0 = chunk.x * cs, y0 = chunk.y * cs;
            int rs = cs + 2 * Margin;
            int ox = x0 - Margin, oy = y0 - Margin;

            // 1) lakes: raw noise, then drop anything thinner than LakeMinWidth
            var lake = new bool[rs, rs];
            if (settings.WaterBiome != null)
            {
                for (int rx = 0; rx < rs; rx++)
                    for (int ry = 0; ry < rs; ry++)
                        lake[rx, ry] = sampler.IsLakeRaw(ox + rx, oy + ry);
                lake = AutoTiler2D.RemoveThinParts(lake, LakeMinWidth);
            }

            // 2) final biome id of every tile in the chunk + margin
            var ids = new byte[rs, rs];
            for (int rx = 0; rx < rs; rx++)
                for (int ry = 0; ry < rs; ry++)
                    ids[rx, ry] = lake[rx, ry] ? WorldSettings2D.WaterId : sampler.SampleLand(ox + rx, oy + ry);

            // 3) base layer
            var baseTiles = new TileBase[cs * cs];
            for (int x = 0; x < cs; x++)
                for (int y = 0; y < cs; y++)
                    baseTiles[x + y * cs] = settings.BiomeById(ids[x + Margin, y + Margin]).BaseTile;

            // 4) overlay layer: one autotiled mask per distinct overlay set
            var overlayTiles = new TileBase[cs * cs];
            var done = new HashSet<AutoTileSet2D>();
            foreach (BiomeData2D biome in AllBiomes(settings))
            {
                AutoTileSet2D set = biome.OverlaySet;
                if (set == null || !done.Add(set)) continue;

                var mask = new bool[rs, rs];
                bool any = false;
                for (int rx = 0; rx < rs; rx++)
                    for (int ry = 0; ry < rs; ry++)
                        if (settings.BiomeById(ids[rx, ry]).OverlaySet == set) { mask[rx, ry] = true; any = true; }
                if (!any) continue;

                mask = AutoTiler2D.RemoveThinParts(mask, 2);
                AutoTiler2D.FillTiles(mask, set, Margin, overlayTiles, cs);
            }

            var bounds = new BoundsInt(x0, y0, 0, cs, cs, 1);
            baseMap.SetTilesBlock(bounds, baseTiles);
            overlayMap.SetTilesBlock(bounds, overlayTiles); // null entries clear old tiles
        }

        static IEnumerable<BiomeData2D> AllBiomes(WorldSettings2D settings)
        {
            foreach (BiomeData2D b in settings.Biomes) yield return b;
            if (settings.WaterBiome != null) yield return settings.WaterBiome;
        }
    }
}