using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameWorld : MonoBehaviour
{
    private const int viewRadius = 5;
    
    public Dictionary<Vector2Int, ChunkData> chunkDatas = new Dictionary<Vector2Int, ChunkData>();
    public ChunkRenderer chunkPrefab;
    public TerrainGenerator generator;

    private Camera mainCamera;
    private Vector2Int currentPlayerChunk;

    private void Start()
    {
        mainCamera = Camera.main;

        ChunkRenderer.InitTriangles();
        
        generator.Init();
        StartCoroutine(Generate(false));
    }

    private IEnumerator Generate(bool wait)
    {
        int loadRadius = viewRadius + 1;
        
        for (int x = currentPlayerChunk.x - loadRadius; x <= currentPlayerChunk.x + loadRadius; x++)
        {
            for (int y = currentPlayerChunk.y - loadRadius; y <= currentPlayerChunk.y + loadRadius; y++)
            {
                Vector2Int chunkPosition = new Vector2Int(x, y);
                if (chunkDatas.ContainsKey(chunkPosition)) continue;

                LoadChunkAt(chunkPosition);

                if (wait) yield return null;
            }
        }
        
        for (int x = currentPlayerChunk.x - viewRadius; x <= currentPlayerChunk.x + viewRadius; x++)
        {
            for (int y = currentPlayerChunk.y - viewRadius; y <= currentPlayerChunk.y + viewRadius; y++)
            {
                Vector2Int chunkPosition = new Vector2Int(x, y);
                ChunkData chunkData = chunkDatas[chunkPosition];
                
                if(chunkData.renderer != null) continue;
                
                SpawnChunkRenderer(chunkData);

                if (wait) yield return new WaitForSecondsRealtime(0.1f);
            }
        }
    }

    [ContextMenu("Regenerate world")]
    public void Regenerate()
    {
        generator.Init();
        
        foreach (var chunkData in chunkDatas)
        {
            Destroy(chunkData.Value.renderer.gameObject);
        }
        
        chunkDatas.Clear();
        
        StartCoroutine(Generate(false));
    }

    private void LoadChunkAt(Vector2Int chunkPosition)
    {
        float xPos = chunkPosition.x * ChunkRenderer.ChunkWidth * ChunkRenderer.BlockScale;
        float zPos = chunkPosition.y * ChunkRenderer.ChunkWidth * ChunkRenderer.BlockScale;

        ChunkData chunkData = new ChunkData();
        chunkData.chunkPosition = chunkPosition;
        chunkData.blocks = generator.GenerateTerrain(xPos, zPos);
        chunkDatas.Add(chunkPosition, chunkData);
    }

    private void SpawnChunkRenderer(ChunkData chunkData)
    {
        float xPos = chunkData.chunkPosition.x * ChunkRenderer.ChunkWidth * ChunkRenderer.BlockScale;
        float zPos = chunkData.chunkPosition.y * ChunkRenderer.ChunkWidth * ChunkRenderer.BlockScale;
        
        ChunkRenderer chunk = Instantiate(chunkPrefab, new Vector3(xPos, 0, zPos), Quaternion.identity, transform);
        chunk.chunkData = chunkData;
        chunk.parentWorld = this;

        chunkData.renderer = chunk;
    }

    private void Update()
    {
        Vector3Int playerWorldPos = Vector3Int.FloorToInt(mainCamera.transform.position / ChunkRenderer.BlockScale);
        Vector2Int playerChunk = GetChunkConteiningBlock(playerWorldPos);
        if (playerChunk != currentPlayerChunk)
        {
            currentPlayerChunk = playerChunk;
            StartCoroutine(Generate(true));
        }
        
        CheckInput();
    }

    private void CheckInput()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            bool isDestroing = Input.GetMouseButtonDown(0);

            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));

            if (Physics.Raycast(ray, out RaycastHit hitInfo))
            {
                Vector3 blockCenter;
                if (isDestroing)
                {
                    blockCenter = hitInfo.point - hitInfo.normal * ChunkRenderer.BlockScale / 2;
                }
                else
                {
                    blockCenter = hitInfo.point + hitInfo.normal * ChunkRenderer.BlockScale / 2;
                }

                Vector3Int blockWorldPos = Vector3Int.FloorToInt(blockCenter / ChunkRenderer.BlockScale);
                Vector2Int chunkPos = GetChunkConteiningBlock(blockWorldPos);
                if (chunkDatas.TryGetValue(chunkPos, out ChunkData chunkData))
                {
                    Vector3Int chunkOrigin = new Vector3Int(chunkPos.x, 0, chunkPos.y) * ChunkRenderer.ChunkWidth;
                    if (isDestroing)
                    {
                        chunkData.renderer.DestroyBlock(blockWorldPos - chunkOrigin);
                    }
                    else
                    {
                        chunkData.renderer.SpawnBlock(blockWorldPos - chunkOrigin);
                    }
                }
            }
        }
    }

    public Vector2Int GetChunkConteiningBlock(Vector3Int blockWorldPos)
    {
        Vector2Int chunkPosition = new Vector2Int(blockWorldPos.x / ChunkRenderer.ChunkWidth, blockWorldPos.z / ChunkRenderer.ChunkWidth);

        if (blockWorldPos.x < 0) chunkPosition.x--;
        if (blockWorldPos.z < 0) chunkPosition.y--;
        
        return chunkPosition;
    }
}
