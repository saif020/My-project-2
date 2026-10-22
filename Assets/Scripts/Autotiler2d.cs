using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProcGen2D
{
    /// Picks the right tile of an AutoTileSet2D from the 8 neighbours of a cell.
    /// Masks are indexed [x, y] with y pointing NORTH (up). Outside the mask counts as "no land".
    public static class AutoTiler2D
    {
        public static AutoTileSlot SelectSlot(bool n, bool e, bool s, bool w,
                                              bool nw, bool ne, bool sw, bool se)
        {
            // convex (outer) corners
            if (!n && !w) return AutoTileSlot.OuterTL;
            if (!n && !e) return AutoTileSlot.OuterTR;
            if (!s && !w) return AutoTileSlot.OuterBL;
            if (!s && !e) return AutoTileSlot.OuterBR;
            // straight edges
            if (!n) return AutoTileSlot.EdgeN;
            if (!s) return AutoTileSlot.EdgeS;
            if (!w) return AutoTileSlot.EdgeW;
            if (!e) return AutoTileSlot.EdgeE;
            // concave (inner) corners. The sheet has only ONE missing diagonal per tile;
            // if two or more are missing we draw the first one found.
            if (!nw) return AutoTileSlot.InnerNW;
            if (!ne) return AutoTileSlot.InnerNE;
            if (!sw) return AutoTileSlot.InnerSW;
            if (!se) return AutoTileSlot.InnerSE;
            return AutoTileSlot.Center;
        }

        /// Keeps only cells that belong to a full minWidth x minWidth block of land.
        /// Removes strips/dots thinner than minWidth. (2 for the grass art, 3 for lakes.)
        public static bool[,] RemoveThinParts(bool[,] mask, int minWidth = 2)
        {
            int w = mask.GetLength(0), h = mask.GetLength(1);
            var keep = new bool[w, h];
            for (int x = 0; x <= w - minWidth; x++)
                for (int y = 0; y <= h - minWidth; y++)
                {
                    bool full = true;
                    for (int i = 0; i < minWidth && full; i++)
                        for (int j = 0; j < minWidth; j++)
                            if (!mask[x + i, y + j]) { full = false; break; }
                    if (!full) continue;

                    for (int i = 0; i < minWidth; i++)
                        for (int j = 0; j < minWidth; j++)
                            keep[x + i, y + j] = true;
                }
            return keep;
        }

        public static void Paint(Tilemap map, bool[,] mask, AutoTileSet2D set, Vector3Int origin)
        {
            int w = mask.GetLength(0), h = mask.GetLength(1);
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    if (!mask[x, y]) continue;
                    AutoTileSlot slot = SelectSlot(
                        Land(mask, x, y + 1), Land(mask, x + 1, y), Land(mask, x, y - 1), Land(mask, x - 1, y),
                        Land(mask, x - 1, y + 1), Land(mask, x + 1, y + 1), Land(mask, x - 1, y - 1), Land(mask, x + 1, y - 1));
                    map.SetTile(origin + new Vector3Int(x, y, 0), set.Get(slot));
                }
        }

        /// Chunk version of Paint: the mask has `margin` extra cells on every side (used only as
        /// neighbour context). Writes the tiles of the inner area into `output`
        /// (index = x + y * innerSize, y up). Cells with no land are left untouched.
        public static void FillTiles(bool[,] mask, AutoTileSet2D set, int margin, TileBase[] output, int innerSize)
        {
            for (int x = 0; x < innerSize; x++)
                for (int y = 0; y < innerSize; y++)
                {
                    int mx = x + margin, my = y + margin;
                    if (!mask[mx, my]) continue;
                    AutoTileSlot slot = SelectSlot(
                        Land(mask, mx, my + 1), Land(mask, mx + 1, my), Land(mask, mx, my - 1), Land(mask, mx - 1, my),
                        Land(mask, mx - 1, my + 1), Land(mask, mx + 1, my + 1), Land(mask, mx - 1, my - 1), Land(mask, mx + 1, my - 1));
                    output[x + y * innerSize] = set.Get(slot);
                }
        }

        /// 47-tile blob mask of cell (x, y) in a land mask (y points north).
        public static int BlobMask(bool[,] m, int x, int y)
            => BlobTileSet2D.Canonical(
                Land(m, x, y + 1), Land(m, x + 1, y + 1), Land(m, x + 1, y), Land(m, x + 1, y - 1),
                Land(m, x, y - 1), Land(m, x - 1, y - 1), Land(m, x - 1, y), Land(m, x - 1, y + 1));

        static bool Land(bool[,] m, int x, int y)
            => x >= 0 && y >= 0 && x < m.GetLength(0) && y < m.GetLength(1) && m[x, y];
    }
}