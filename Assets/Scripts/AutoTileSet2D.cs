using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProcGen2D
{
    /// The 13 tiles of one terrain overlay (e.g. light grass).
    /// "EdgeN" = the edge on the NORTH side of a patch (land ends going north), and so on.
    public enum AutoTileSlot
    {
        Center,
        EdgeN, EdgeE, EdgeS, EdgeW,
        OuterTL, OuterTR, OuterBL, OuterBR,   // convex corners
        InnerNW, InnerNE, InnerSW, InnerSE    // concave corners
    }

    /// Created by AutoTileSetBuilder2D. Tiles are indexed by (int)AutoTileSlot.
    public class AutoTileSet2D : ScriptableObject
    {
        public const int SlotCount = 13;
        public Tile[] Tiles = new Tile[SlotCount];

        public Tile Get(AutoTileSlot slot) => Tiles[(int)slot];
    }
}