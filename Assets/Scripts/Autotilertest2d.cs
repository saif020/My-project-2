using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProcGen2D
{
    /// Step 3 test: paints a hand-written shape ('#' = land) with the autotiler.
    /// Edit the shape / settings in Play mode and it repaints by itself.
    public class AutoTilerTest2D : MonoBehaviour
    {
        public Tilemap BaseMap;      // bottom layer, filled with BaseTile
        public Tilemap OverlayMap;   // above BaseMap (higher Order in Layer), gets the autotiled land
        public TileBase BaseTile;    // any plain ground tile from your Tile Palette (e.g. dirt)
        public AutoTileSet2D Set;    // e.g. Grass_Light
        public Vector3Int Origin = Vector3Int.zero;
        public bool RemoveThin = true;

        [TextArea(12, 30)]
        public string Shape =
@"..........
..####....
..#####...
.#######..
.#######..
..#####...
..#####.##
.......###
.......###
..........";

        string lastShape;
        bool lastRemoveThin;
        Vector3Int lastOrigin;
        AutoTileSet2D lastSet;
        TileBase lastBaseTile;
        bool painted;

        void Update()
        {
            // Compare the objects themselves (no GetInstanceID, which is obsolete in newer Unity versions).
            bool changed = !painted
                || Shape != lastShape
                || RemoveThin != lastRemoveThin
                || Origin != lastOrigin
                || Set != lastSet
                || BaseTile != lastBaseTile;
            if (!changed) return;

            painted = true;
            lastShape = Shape;
            lastRemoveThin = RemoveThin;
            lastOrigin = Origin;
            lastSet = Set;
            lastBaseTile = BaseTile;
            Repaint();
        }

        void Repaint()
        {
            if (BaseMap == null || OverlayMap == null || Set == null)
            {
                Debug.LogError("AutoTilerTest2D: assign BaseMap, OverlayMap and Set.");
                return;
            }

            string[] rows = Shape.Replace("\r", "").Split('\n');
            int h = rows.Length, w = 0;
            foreach (string r in rows) w = Mathf.Max(w, r.Length);

            // text row 0 is the TOP of the picture, so it becomes the highest y
            var mask = new bool[w, h];
            for (int row = 0; row < h; row++)
                for (int x = 0; x < rows[row].Length; x++)
                    mask[x, h - 1 - row] = rows[row][x] == '#';

            if (RemoveThin) mask = AutoTiler2D.RemoveThinParts(mask);

            BaseMap.ClearAllTiles();
            OverlayMap.ClearAllTiles();

            if (BaseTile != null)
                for (int x = -1; x <= w; x++)
                    for (int y = -1; y <= h; y++)
                        BaseMap.SetTile(Origin + new Vector3Int(x, y, 0), BaseTile);

            AutoTiler2D.Paint(OverlayMap, mask, Set, Origin);
        }
    }
}