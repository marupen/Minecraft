using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ChunkRenderer : MonoBehaviour
{
    public const int ChunkWidth = 32;
    public const int ChunkWidthSq = ChunkWidth * ChunkWidth;
    public const int ChunkHeight = 128;
    public const float BlockScale = 0.125f;

    public ChunkData chunkData;
    public GameWorld parentWorld;

    public BlockDatabase blocks;

    private Mesh chunkMesh;

    private ChunkData leftChunk;
    private ChunkData rightChunk;
    private ChunkData fwdChunk;
    private ChunkData backChunk;

    private List<Vector3> verticles = new List<Vector3>();
    private List<Vector2> uvs = new List<Vector2>();

    private static int[] triangles;

    private static ProfilerMarker MeshingMarker = new ProfilerMarker(ProfilerCategory.Loading, "Meshing");

    public static void InitTriangles()
    {
        triangles = new int[65536 * 6 / 4];

        int vertexNum = 4;
        for (int i = 0; i < triangles.Length; i += 6)
        {
            triangles[i] = vertexNum - 4;
            triangles[i+1] = vertexNum - 3;
            triangles[i+2] = vertexNum - 2;

            triangles[i+3] = vertexNum - 2;
            triangles[i+4] = vertexNum - 3;
            triangles[i+5] = vertexNum - 1;
            vertexNum += 4;
        }
    }
    
    private void Start()
    {
        parentWorld.chunkDatas.TryGetValue(chunkData.chunkPosition + Vector2Int.left, out leftChunk);
        parentWorld.chunkDatas.TryGetValue(chunkData.chunkPosition + Vector2Int.right, out rightChunk);
        parentWorld.chunkDatas.TryGetValue(chunkData.chunkPosition + Vector2Int.up, out fwdChunk);
        parentWorld.chunkDatas.TryGetValue(chunkData.chunkPosition + Vector2Int.down, out backChunk);
        
        chunkMesh = new Mesh();
        RegenerateMesh();

        GetComponent<MeshFilter>().sharedMesh = chunkMesh;
    }

    private void RegenerateMesh()
    {
        MeshingMarker.Begin();
        
        verticles.Clear();
        uvs.Clear();

        int maxY = 0;
        for (int y = 0; y < ChunkHeight; y++)
        {
            for (int x = 0; x < ChunkWidth; x++)
            {
                for (int z = 0; z < ChunkWidth; z++)
                {
                    if (GenerateBlock(x, y, z))
                    {
                        if (maxY < y) maxY = y;
                    }
                }
            }
        }

        chunkMesh.triangles = Array.Empty<int>();
        chunkMesh.vertices = verticles.ToArray();
        chunkMesh.uv = uvs.ToArray();
        chunkMesh.SetTriangles(triangles, 0, verticles.Count * 6 / 4, 0, false); 

        chunkMesh.Optimize();

        chunkMesh.RecalculateNormals();
        
        Vector3 boundsSize = new Vector3(ChunkWidth, maxY, ChunkWidth) * BlockScale;
        chunkMesh.bounds = new Bounds(boundsSize / 2, boundsSize);

        GetComponent<MeshCollider>().sharedMesh = chunkMesh;
        
        MeshingMarker.End();
    }

    public void SpawnBlock(Vector3Int blockPosition)
    {
        int index = blockPosition.x + blockPosition.y * ChunkWidthSq + blockPosition.z * ChunkWidth;
        
        chunkData.blocks[index] = BlockType.Wood;
        RegenerateMesh();
    }

    public void DestroyBlock(Vector3Int blockPosition)
    {
        int index = blockPosition.x + blockPosition.y * ChunkWidthSq + blockPosition.z * ChunkWidth;
        
        chunkData.blocks[index] = BlockType.Air;
        RegenerateMesh();
    }

    bool GenerateBlock(int x, int y, int z)
    {
        int index = x + y * ChunkWidthSq + z * ChunkWidth;
        if (chunkData.blocks[index] == 0) return false;

        Vector3Int blockPosition = new Vector3Int(x, y, z);
        if (GetBlockAtPosition(blockPosition + Vector3Int.right) == 0)
        {
            GenerateRightSide(blockPosition);
            AddUvs(GetBlockAtPosition(blockPosition), Vector3Int.right);
        }
        if (GetBlockAtPosition(blockPosition + Vector3Int.left) == 0)
        {
            GenerateLeftSide(blockPosition);
            AddUvs(GetBlockAtPosition(blockPosition), Vector3Int.left);
        }
        if (GetBlockAtPosition(blockPosition + Vector3Int.forward) == 0)
        {
            GenerateFrontSide(blockPosition);
            AddUvs(GetBlockAtPosition(blockPosition), Vector3Int.forward);
        }
        if (GetBlockAtPosition(blockPosition + Vector3Int.back) == 0)
        {
            GenerateBackSide(blockPosition);
            AddUvs(GetBlockAtPosition(blockPosition), Vector3Int.back);
        }
        if (GetBlockAtPosition(blockPosition + Vector3Int.up) == 0)
        {
            GenerateTopSide(blockPosition);
            AddUvs(GetBlockAtPosition(blockPosition), Vector3Int.up);
        }
        if (blockPosition.y > 0 && GetBlockAtPosition(blockPosition + Vector3Int.down) == 0)
        {
            GenerateBottomSide(blockPosition);
            AddUvs(GetBlockAtPosition(blockPosition), Vector3Int.down);
        }

        return true;
    }

    private BlockType GetBlockAtPosition(Vector3Int blockPosition)
    {
        if(blockPosition.x >= 0 && blockPosition.x < ChunkWidth &&
           blockPosition.y >= 0 && blockPosition.y < ChunkHeight &&
           blockPosition.z >= 0 && blockPosition.z < ChunkWidth)
        {
            int index = blockPosition.x + blockPosition.y * ChunkWidthSq + blockPosition.z * ChunkWidth;
            return chunkData.blocks[index];
        }
        else
        {
            if (blockPosition.y < 0 || blockPosition.y >= ChunkHeight) return BlockType.Air;

            if(blockPosition.x < 0)
            {
                if (leftChunk == null)
                {
                    return BlockType.Air;
                }
                
                blockPosition.x += ChunkWidth;
                int index = blockPosition.x + blockPosition.y * ChunkWidthSq + blockPosition.z * ChunkWidth;
                return leftChunk.blocks[index];
            }
            if(blockPosition.x >= ChunkWidth)
            {
                if (rightChunk == null)
                {
                    return BlockType.Air;
                }
                
                blockPosition.x -= ChunkWidth;
                int index = blockPosition.x + blockPosition.y * ChunkWidthSq + blockPosition.z * ChunkWidth;
                return rightChunk.blocks[index];
            }
            if (blockPosition.z < 0)
            {
                if (backChunk == null)
                {
                    return BlockType.Air;
                }
                
                blockPosition.z += ChunkWidth;
                int index = blockPosition.x + blockPosition.y * ChunkWidthSq + blockPosition.z * ChunkWidth;
                return backChunk.blocks[index];
            }
            if (blockPosition.z >= ChunkWidth)
            {
                if (fwdChunk == null)
                {
                    return BlockType.Air;
                }
                
                blockPosition.z -= ChunkWidth;
                int index = blockPosition.x + blockPosition.y * ChunkWidthSq + blockPosition.z * ChunkWidth;
                return fwdChunk.blocks[index];
            }

            return BlockType.Air;
        }
    }

    private void GenerateRightSide(Vector3Int blockPosition)
    {
        verticles.Add((new Vector3(1, 0, 0) + blockPosition) * BlockScale);
        verticles.Add((new Vector3(1, 1, 0) + blockPosition) * BlockScale);
        verticles.Add((new Vector3(1, 0, 1) + blockPosition) * BlockScale);
        verticles.Add((new Vector3(1, 1, 1) + blockPosition) * BlockScale);
    }

    private void GenerateLeftSide(Vector3Int blockPosition)
    {
        verticles.Add((new Vector3(0, 0, 0) + blockPosition) * BlockScale);
        verticles.Add((new Vector3(0, 0, 1) + blockPosition) * BlockScale);
        verticles.Add((new Vector3(0, 1, 0) + blockPosition) * BlockScale);
        verticles.Add((new Vector3(0, 1, 1) + blockPosition) * BlockScale);
    }

    private void GenerateFrontSide(Vector3Int blockPosition)
    {
        verticles.Add((new Vector3(0, 0, 1) + blockPosition) * BlockScale);
        verticles.Add((new Vector3(1, 0, 1) + blockPosition) * BlockScale);
        verticles.Add((new Vector3(0, 1, 1) + blockPosition) * BlockScale);
        verticles.Add((new Vector3(1, 1, 1) + blockPosition) * BlockScale);
    }

    private void GenerateBackSide(Vector3Int blockPosition)
    {
        verticles.Add((new Vector3(1, 0, 0) + blockPosition) * BlockScale);
        verticles.Add((new Vector3(0, 0, 0) + blockPosition) * BlockScale);
        verticles.Add((new Vector3(1, 1, 0) + blockPosition) * BlockScale);
        verticles.Add((new Vector3(0, 1, 0) + blockPosition) * BlockScale);
    }

    private void GenerateTopSide(Vector3Int blockPosition)
    {
        verticles.Add((new Vector3(1, 1, 0) + blockPosition) * BlockScale);
        verticles.Add((new Vector3(0, 1, 0) + blockPosition) * BlockScale);
        verticles.Add((new Vector3(1, 1, 1) + blockPosition) * BlockScale);
        verticles.Add((new Vector3(0, 1, 1) + blockPosition) * BlockScale);
    }

    private void GenerateBottomSide(Vector3Int blockPosition)
    {
        verticles.Add((new Vector3(1, 0, 0) + blockPosition) * BlockScale);
        verticles.Add((new Vector3(1, 0, 1) + blockPosition) * BlockScale);
        verticles.Add((new Vector3(0, 0, 0) + blockPosition) * BlockScale);
        verticles.Add((new Vector3(0, 0, 1) + blockPosition) * BlockScale);
    }

    private void AddUvs(BlockType blockType, Vector3Int normal)
    {
        Vector2 uv;

        BlockInfo info = blocks.GetInfo(blockType);

        if (info != null)
        {
            uv = info.GetPixelOffset(normal) / 256;
        }
        else
        {
            uv = new Vector2(0f / 256, 192f / 256);
        }

        /*if(blockType == BlockType.Grass)
        {
            uv = normal == Vector3Int.up ? new Vector2(0f / 256, 240f / 256) :
                 normal == Vector3Int.down ? new Vector2(32f / 256, 240f / 256) :
                 new Vector2(48f / 256, 240f / 256);
        }*/

        for (int i = 0; i < 4; i++)
        {
            uvs.Add(uv);
        }
    }
}
