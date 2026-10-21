using System.Collections.Generic;
using UnityEngine;

namespace ProcGen2D
{
    [CreateAssetMenu(menuName = "ProcGen2D/World Settings", fileName = "WorldSettings2D")]
    public class WorldSettings2D : ScriptableObject
    {
        /// Biome id used for lake tiles (lakes are not part of the Biomes list).
        public const byte WaterId = 255;

        public int Seed = 12345;

        [Header("World size (1 tile = 1 Unity unit = 32 px)")]
        [Tooltip("Use a multiple of Chunk Size (e.g. 2048 = 32 x 32 chunks of 64).")]
        public int WorldSizeInTiles = 2048;
        [Min(8)] public int ChunkSizeInTiles = 64;

        [Tooltip("Ooze runs on a coarse grid: 1 cell = this many tiles.")]
        [Min(1)] public int LowResCellSizeInTiles = 1;

        [Header("Ooze")]
        [Range(0.0001f, 0.5f)] public float SeedPointDensity = 0.01f;

        [Header("Biome borders (turn blocky cells into natural shapes)")]
        [Tooltip("How far (in tiles) a border can be pushed around by noise.")]
        [Min(0f)] public float BorderWarpAmplitude = 6f;
        [Tooltip("Noise frequency. Lower = broader wiggles.")]
        [Min(0.001f)] public float BorderWarpFrequency = 0.06f;

        [Header("Lakes (noise based, NOT part of the Ooze biomes list)")]
        [Tooltip("Biome data used for lake tiles. Leave empty for a world without lakes. Do not also put it in the Biomes list.")]
        public BiomeData2D WaterBiome;
        [Tooltip("Lower = broader lakes. Wavelength is about 1 / frequency tiles.")]
        [Min(0.001f)] public float LakeFrequency = 0.015f;
        [Tooltip("Water where the lake noise is below this. Higher = more water (0.34 ~ 8%, 0.38 ~ 14%, 0.42 ~ 23%, rough estimates).")]
        [Range(0f, 1f)] public float LakeThreshold = 0.38f;

        public List<BiomeData2D> Biomes = new List<BiomeData2D>();

        public int LowResSize => Mathf.CeilToInt((float)WorldSizeInTiles / LowResCellSizeInTiles);
        public int ChunksPerSide => Mathf.CeilToInt((float)WorldSizeInTiles / ChunkSizeInTiles);

        public BiomeData2D BiomeById(byte id) => id == WaterId ? WaterBiome : Biomes[id];
    }
}