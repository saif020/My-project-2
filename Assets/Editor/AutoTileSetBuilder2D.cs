using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProcGen2D.EditorTools
{
    /// Cuts the 13 tiles out of each 5x5 "cross" piece of the ForgottenMemories sheet.
    /// Tiles are found by POSITION in the sheet (not by sprite name), so re-slicing is safe.
    /// Requires the texture to be sliced: Sprite Mode = Multiple, Grid By Cell Size 32x32, then Apply.
    [CreateAssetMenu(menuName = "ProcGen2D/AutoTile Builder", fileName = "AutoTileBuilder2D")]
    public class AutoTileSetBuilder2D : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public string Name;
            [Tooltip("Top-left TILE (column,row) of the 5x5 cross piece, counted from the top-left of the sheet.")]
            public Vector2Int Origin;
        }

        public Texture2D Source;
        public int TileSize = 32;
        public string OutputFolder = "Assets/World Data/AutoTiles";

        public List<Entry> Entries = new List<Entry>
        {
            new Entry { Name = "Grass_Light",  Origin = new Vector2Int(0, 5)  },
            new Entry { Name = "Dirt_Overlay", Origin = new Vector2Int(10, 5) },
            new Entry { Name = "Grass_Edged",  Origin = new Vector2Int(0, 10) },
        };

        // Offset (col,row) of each slot inside the 5x5 cross piece. Same order as AutoTileSlot.
        static readonly Vector2Int[] SlotOffsets =
        {
            new Vector2Int(2, 2),                                                                   // Center
            new Vector2Int(2, 0), new Vector2Int(4, 2), new Vector2Int(2, 4), new Vector2Int(0, 2), // EdgeN, E, S, W
            new Vector2Int(1, 0), new Vector2Int(3, 0), new Vector2Int(0, 3), new Vector2Int(4, 3), // OuterTL, TR, BL, BR
            new Vector2Int(1, 1), new Vector2Int(3, 1), new Vector2Int(1, 3), new Vector2Int(3, 3), // InnerNW, NE, SW, SE
        };

        public void Build()
        {
            if (Source == null) { Debug.LogError("AutoTileSetBuilder2D: assign the Source texture."); return; }

            string texPath = AssetDatabase.GetAssetPath(Source);
            List<Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(texPath).OfType<Sprite>().ToList();
            if (sprites.Count == 0)
            {
                Debug.LogError("AutoTileSetBuilder2D: the texture has no sprites. Set Sprite Mode = Multiple, slice 32x32, Apply.");
                return;
            }

            int texH = Source.height;
            Directory.CreateDirectory(OutputFolder);
            AssetDatabase.Refresh();

            int built = 0, missing = 0;
            foreach (Entry entry in Entries)
            {
                string folder = $"{OutputFolder}/{entry.Name}";
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();

                var set = LoadOrCreate<AutoTileSet2D>($"{OutputFolder}/{entry.Name}.asset");
                set.Tiles = new Tile[AutoTileSet2D.SlotCount];

                for (int i = 0; i < AutoTileSet2D.SlotCount; i++)
                {
                    int col = entry.Origin.x + SlotOffsets[i].x;
                    int row = entry.Origin.y + SlotOffsets[i].y;
                    Sprite sprite = FindSprite(sprites, col, row, texH);
                    if (sprite == null)
                    {
                        Debug.LogError($"{entry.Name}: no sprite at tile ({col},{row}) for slot {(AutoTileSlot)i}. Is the sheet sliced at {TileSize}x{TileSize}?");
                        missing++;
                        continue;
                    }

                    var tile = LoadOrCreate<Tile>($"{folder}/{entry.Name}_{(AutoTileSlot)i}.asset");
                    tile.sprite = sprite;
                    tile.colliderType = Tile.ColliderType.None;
                    EditorUtility.SetDirty(tile);
                    set.Tiles[i] = tile;
                    built++;
                }
                EditorUtility.SetDirty(set);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"AutoTileSetBuilder2D: {built} tiles built, {missing} missing.");
        }

        Sprite FindSprite(List<Sprite> sprites, int col, int row, int texH)
        {
            // Sprite rects use a bottom-left origin; "row" is counted from the top.
            float x = col * TileSize;
            float y = texH - (row + 1) * TileSize;
            return sprites.FirstOrDefault(s =>
                Mathf.Approximately(s.rect.x, x) && Mathf.Approximately(s.rect.y, y) &&
                Mathf.Approximately(s.rect.width, TileSize) && Mathf.Approximately(s.rect.height, TileSize));
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }
    }

    [CustomEditor(typeof(AutoTileSetBuilder2D))]
    public class AutoTileSetBuilder2DEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Build Tiles"))
                ((AutoTileSetBuilder2D)target).Build();
        }
    }
}