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

            // 2) the Ooze (land) biome under every tile, and the final id (lake or land)
            var land = new byte[rs, rs];
            var ids = new byte[rs, rs];
            for (int rx = 0; rx < rs; rx++)
                for (int ry = 0; ry < rs; ry++)
                {
                    land[rx, ry] = sampler.SampleLand(ox + rx, oy + ry);
                    ids[rx, ry] = lake[rx, ry] ? WorldSettings2D.WaterId : land[rx, ry];
                }

            // 3) base layer. Under a lake with a shore set we keep the LAND tile (the shore is transparent
            //    outside the water). Without a shore set the lake is a plain water tile.
            var baseTiles = new TileBase[cs * cs];
            for (int x = 0; x < cs; x++)
                for (int y = 0; y < cs; y++)
                {
                    int rx = x + Margin, ry = y + Margin;
                    TileBase t;
                    if (ids[rx, ry] == WorldSettings2D.WaterId)
                        t = ShoreFor(settings, land[rx, ry]) != null
                            ? settings.Biomes[land[rx, ry]].BaseTile
                            : settings.WaterBiome.BaseTile;
                    else
                        t = settings.Biomes[ids[rx, ry]].BaseTile;
                    baseTiles[x + y * cs] = t;
                }

            // 4) overlay layer: autotiled land overlays (e.g. grass), one mask per distinct set
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

            // 5) lake shores: connectivity comes from the WHOLE lake mask, the style (tile set) from the land biome
            //    under each cell, so a lake crossing two biomes switches style instead of drawing a shore inside the water.
            for (int x = 0; x < cs; x++)
                for (int y = 0; y < cs; y++)
                {
                    int rx = x + Margin, ry = y + Margin;
                    if (!lake[rx, ry]) continue;
                    BlobTileSet2D shore = ShoreFor(settings, land[rx, ry]);
                    if (shore == null) continue;
                    overlayTiles[x + y * cs] = shore.Get(AutoTiler2D.BlobMask(lake, rx, ry));
                }

            var bounds = new BoundsInt(x0, y0, 0, cs, cs, 1);
            baseMap.SetTilesBlock(bounds, baseTiles);
            overlayMap.SetTilesBlock(bounds, overlayTiles); // null entries clear old tiles
        }

        static BlobTileSet2D ShoreFor(WorldSettings2D settings, byte landId)
        {
            BlobTileSet2D own = settings.Biomes[landId].ShoreSet;
            return own != null ? own : settings.DefaultShoreSet;
        }

        static IEnumerable<BiomeData2D> AllBiomes(WorldSettings2D settings)
        {
            foreach (BiomeData2D b in settings.Biomes) yield return b;
            if (settings.WaterBiome != null) yield return settings.WaterBiome;
        }
    }
}