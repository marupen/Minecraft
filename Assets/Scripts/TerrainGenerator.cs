using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

[CreateAssetMenu(menuName = "Terrain generator")]
public class TerrainGenerator : ScriptableObject
{
    public float baseHeight = 8;
    public NoiseOctaveSettings[] octaves;
    public NoiseOctaveSettings domainWarp;
    
    [Serializable]
    public class NoiseOctaveSettings
    {
        public FastNoiseLite.NoiseType noiseType;
        public float frequency = 0.2f;
        public float amplitude = 1;
    }

    private FastNoiseLite[] octaveNoises;
    private FastNoiseLite warpNoise;

    private static ProfilerMarker GeneratingMarker = new ProfilerMarker(ProfilerCategory.Loading, "Generating");

    public void Init()
    {
        octaveNoises = new FastNoiseLite[octaves.Length];
        for (int i = 0; i < octaves.Length; i++)
        {
            octaveNoises[i] = new FastNoiseLite();
            octaveNoises[i].SetNoiseType(octaves[i].noiseType);
            octaveNoises[i].SetFrequency(octaves[i].frequency);
        }
        
        warpNoise = new FastNoiseLite();
        warpNoise.SetNoiseType(domainWarp.noiseType);
        warpNoise.SetFrequency(domainWarp.frequency);
        warpNoise.SetDomainWarpAmp(domainWarp.amplitude);
    }
    
    public BlockType[] GenerateTerrain(float xOffset, float zOffset)
    {
        GeneratingMarker.Begin();
        BlockType[] result = new BlockType[ChunkRenderer.ChunkWidth * ChunkRenderer.ChunkHeight * ChunkRenderer.ChunkWidth];

        for(int x = 0; x < ChunkRenderer.ChunkWidth; x++)
        {
            for(int z = 0; z < ChunkRenderer.ChunkWidth; z++)
            {
                //float height = Mathf.PerlinNoise((x * ChunkRenderer.blockScale + xOffset) * 0.17f, (z * ChunkRenderer.blockScale + zOffset) * 0.17f) * 10 + 10;
                float worldX = x * ChunkRenderer.BlockScale + xOffset;
                float worldZ = z * ChunkRenderer.BlockScale + zOffset;
                
                float height = GetHeight(worldX, worldZ);
                float grassLayerHeight = 1 + octaveNoises[0].GetNoise(worldX, worldZ) * 0.2f;
                float bedrockLayerHeight = 0.5f + octaveNoises[0].GetNoise(worldX, worldZ) * 0.2f;
                
                for(int y = 0; y < height / ChunkRenderer.BlockScale; y++)
                {
                    int index = x + y * ChunkRenderer.ChunkWidthSq + z * ChunkRenderer.ChunkWidth;
                    
                    if (height - y * ChunkRenderer.BlockScale < grassLayerHeight)
                    {
                        result[index] = BlockType.Grass;
                    }
                    else if (y * ChunkRenderer.BlockScale < bedrockLayerHeight)
                    {
                        result[index] = BlockType.Wood;
                    }
                    else
                    {
                        result[index] = BlockType.Stone;
                    }
                }
            }
        }
        
        GeneratingMarker.End();
        return result;
    }

    private float GetHeight(float x, float y)
    {
        warpNoise.DomainWarp(ref x, ref y);
        
        float result = baseHeight;

        for (int i = 0; i < octaves.Length; i++)
        {
            float noise = octaveNoises[i].GetNoise(x, y);
            result += noise * octaves[i].amplitude / 2;
        }

        return result;
    }
}
