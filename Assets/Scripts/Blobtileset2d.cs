using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProcGen2D
{
    /// A full 47-tile "blob" autotile set (used for water shores).
    /// Tiles are indexed by a neighbour bitmask; diagonals only count when both adjacent sides are set,
    /// which is what reduces the 256 possible masks to 47.
    public class BlobTileSet2D : ScriptableObject
    {
        public const int N = 1, NE = 2, E = 4, SE = 8, S = 16, SW = 32, W = 64, NW = 128;

        public Tile[] Tiles = new Tile[256];

        public Tile Get(int mask) => Tiles[mask];

        public static int Canonical(bool n, bool ne, bool e, bool se, bool s, bool sw, bool w, bool nw)
        {
            int m = 0;
            if (n) m |= N;
            if (e) m |= E;
            if (s) m |= S;
            if (w) m |= W;
            if (n && e && ne) m |= NE;
            if (e && s && se) m |= SE;
            if (s && w && sw) m |= SW;
            if (w && n && nw) m |= NW;
            return m;
        }
    }
}