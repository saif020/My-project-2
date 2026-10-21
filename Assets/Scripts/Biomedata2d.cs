using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProcGen2D
{
    [CreateAssetMenu(menuName = "ProcGen2D/Biome Data", fileName = "NewBiome2D")]
    public class BiomeData2D : ScriptableObject
    {
        public string DisplayName = "New Biome";
        public Color DebugColor = Color.white;

        [Tooltip("Relative chance of being picked for a seed point.")]
        [Min(0f)] public float SelectionWeight = 1f;

        [Header("Ooze (see procjam.com/tutorials/en/ooze)")]
        [Range(0f, 2f)] public float MinIntensity = 0.8f;
        [Range(0f, 2f)] public float MaxIntensity = 1.0f;
        [Range(0.001f, 0.5f)] public float MinDecayRate = 0.02f;
        [Range(0.001f, 0.5f)] public float MaxDecayRate = 0.06f;

        [Header("Tiles")]
        [Tooltip("Ground tile of this biome (bottom layer). Water biome: the water tile. Grass biome: the dirt tile.")]
        public TileBase BaseTile;
        [Tooltip("Optional autotiled layer drawn above the base (e.g. Grass_Light on a grass biome).")]
        public AutoTileSet2D OverlaySet;
    }
}