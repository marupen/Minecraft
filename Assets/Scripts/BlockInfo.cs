using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Blocks/Normal block")]
public class BlockInfo : ScriptableObject
{
    public BlockType type;
    public Vector2 pixelsOffset;

    public AudioClip stepSound;
    public float timeToBreak = 0.3f;

    public virtual Vector2 GetPixelOffset(Vector3Int normal)
    {
        return pixelsOffset;
    }
}
