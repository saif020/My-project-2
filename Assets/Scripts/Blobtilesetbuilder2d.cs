using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProcGen2D.EditorTools
{
    /// Builds BlobTileSet2D assets from the Pipoya water sheet (_A_Water_pipo.png).
    /// Each group is 8x6 tiles = 47 blob tiles + 1 empty cell. The builder works out which neighbour mask
    /// every tile belongs to by reading the tile's own pixels (which edges/corners are opaque),
    /// so it does not depend on the order the artist used.
    /// Requires the texture to be sliced: Sprite Mode = Multiple, Grid By Cell Size 32x32, Apply.
    [CreateAssetMenu(menuName = "ProcGen2D/Blob Tile Builder", fileName = "BlobTileBuilder2D")]
    public class BlobTileSetBuilder2D : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public string Name;
            [Tooltip("Top-left TILE (column,row) of the 8x6 group in animation frame 0.")]
            public Vector2Int Origin;
        }

        public Texture2D Source;
        public int TileSize = 32;
        [Tooltip("Animation frame to use (the sheet has 8, each 8 tiles wide). Animation itself comes later.")]
        [Range(0, 7)] public int Frame = 0;
        public int FrameWidthInTiles = 8;
        public string OutputFolder = "Assets/World Data/AutoTiles";

        public List<Entry> Entries = new List<Entry>
        {
            new Entry { Name = "Water_Rock_Dark",  Origin = new Vector2Int(0, 0)  },
            new Entry { Name = "Water_Rock_Light", Origin = new Vector2Int(0, 6)  },
            new Entry { Name = "Water_Foam",       Origin = new Vector2Int(0, 42) },
        };

        public void Build()
        {
            if (Source == null) { Debug.LogError("BlobTileSetBuilder2D: assign the Source texture."); return; }

            string texPath = AssetDatabase.GetAssetPath(Source);
            List<Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(texPath).OfType<Sprite>().ToList();
            if (sprites.Count == 0)
            {
                Debug.LogError("BlobTileSetBuilder2D: the texture has no sprites. Set Sprite Mode = Multiple, slice 32x32, Apply.");
                return;
            }

            // Read the pixels from the file itself (works even if Read/Write is off on the import settings).
            var pixelTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(pixelTex, File.ReadAllBytes(texPath));
            Color32[] px = pixelTex.GetPixels32();
            int texW = pixelTex.width, texH = pixelTex.height;
            if (texH != Source.height)
                Debug.LogWarning($"Imported height ({Source.height}) differs from the file ({texH}). Set Max Size >= {texH} on the texture.");

            Directory.CreateDirectory(OutputFolder);
            AssetDatabase.Refresh();

            int built = 0;
            foreach (Entry entry in Entries)
            {
                int col0 = entry.Origin.x + Frame * FrameWidthInTiles;
                int row0 = entry.Origin.y;

                var found = new Dictionary<int, Vector2Int>();
                for (int r = 0; r < 6; r++)
                    for (int c = 0; c < FrameWidthInTiles; c++)
                    {
                        int col = col0 + c, row = row0 + r;
                        if (Frac(px, texW, texH, col, row, 0, TileSize, 0, TileSize) < 0.02f) continue; // empty cell

                        int mask = MaskOfCell(px, texW, texH, col, row);
                        if (found.ContainsKey(mask))
                            Debug.LogError($"{entry.Name}: two tiles have the same mask {mask} (cells {found[mask]} and ({col},{row})).");
                        else
                            found[mask] = new Vector2Int(col, row);
                    }

                if (found.Count != 47)
                    Debug.LogError($"{entry.Name}: expected 47 tiles, found {found.Count}. Is the Origin right?");

                string folder = $"{OutputFolder}/{entry.Name}";
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();

                var set = LoadOrCreate<BlobTileSet2D>($"{OutputFolder}/{entry.Name}.asset");
                set.Tiles = new Tile[256];

                foreach (var kv in found)
                {
                    Sprite sprite = FindSprite(sprites, kv.Value.x, kv.Value.y, Source.height);
                    if (sprite == null)
                    {
                        Debug.LogError($"{entry.Name}: no sprite at tile ({kv.Value.x},{kv.Value.y}). Is the sheet sliced at {TileSize}x{TileSize}?");
                        continue;
                    }
                    var tile = LoadOrCreate<Tile>($"{folder}/{entry.Name}_{kv.Key:D3}.asset");
                    tile.sprite = sprite;
                    tile.colliderType = Tile.ColliderType.None;
                    EditorUtility.SetDirty(tile);
                    set.Tiles[kv.Key] = tile;
                    built++;
                }
                EditorUtility.SetDirty(set);
            }

            Object.DestroyImmediate(pixelTex);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"BlobTileSetBuilder2D: {built} tiles built.");
        }

        // Which neighbours does this tile connect to? An edge counts as connected when it is opaque.
        int MaskOfCell(Color32[] px, int w, int h, int col, int row)
        {
            int T = TileSize, q1 = T / 4, q3 = T * 3 / 4, e = 2, k = 3;
            bool n = Frac(px, w, h, col, row, q1, q3, 0, e) > 0.5f;
            bool s = Frac(px, w, h, col, row, q1, q3, T - e, T) > 0.5f;
            bool wl = Frac(px, w, h, col, row, 0, e, q1, q3) > 0.5f;
            bool er = Frac(px, w, h, col, row, T - e, T, q1, q3) > 0.5f;
            bool nw = Frac(px, w, h, col, row, 0, k, 0, k) > 0.5f;
            bool ne = Frac(px, w, h, col, row, T - k, T, 0, k) > 0.5f;
            bool sw = Frac(px, w, h, col, row, 0, k, T - k, T) > 0.5f;
            bool se = Frac(px, w, h, col, row, T - k, T, T - k, T) > 0.5f;
            return BlobTileSet2D.Canonical(n, ne, er, se, s, sw, wl, nw);
        }

        // Fraction of opaque pixels in a rectangle of one cell. x/y are measured from the cell's TOP-left.
        float Frac(Color32[] px, int texW, int texH, int col, int row, int x0, int x1, int y0, int y1)
        {
            int count = 0, opaque = 0;
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    int tx = col * TileSize + x;
                    int ty = texH - 1 - (row * TileSize + y); // pixel rows in Unity start at the BOTTOM
                    count++;
                    if (px[ty * texW + tx].a > 128) opaque++;
                }
            return count == 0 ? 0f : (float)opaque / count;
        }

        Sprite FindSprite(List<Sprite> sprites, int col, int row, int texH)
        {
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

    [CustomEditor(typeof(BlobTileSetBuilder2D))]
    public class BlobTileSetBuilder2DEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Build Tiles"))
                ((BlobTileSetBuilder2D)target).Build();
        }
    }
}