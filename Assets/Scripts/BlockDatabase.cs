using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockDatabase : ScriptableObject
{
    [SerializeField] private BlockInfo[] blocks;

    private Dictionary<BlockType, BlockInfo> blocksCached = new Dictionary<BlockType, BlockInfo>();

    private void Init()
    {
        blocksCached.Clear();
        
        foreach (BlockInfo blockInfo in blocks)
        {
            blocksCached.Add(blockInfo.type, blockInfo);
        }
    }

    public BlockInfo GetInfo(BlockType type)
    {
        if(blocksCached.Count == 0) Init();
        
        if (blocksCached.TryGetValue(type, out BlockInfo blockInfo))
        {
            return blockInfo;
        }

        return null;
    }
}
