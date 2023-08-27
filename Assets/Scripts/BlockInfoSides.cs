using UnityEngine;

[CreateAssetMenu(menuName = "Blocks/Sides")]
public class BlockInfoSides: BlockInfo
{
    public Vector2 pixelsOffsetUp;
    public Vector2 pixelsOffsetDown;

    public override Vector2 GetPixelOffset(Vector3Int normal)
    {
        if (normal == Vector3Int.up) return pixelsOffsetUp;
        if (normal == Vector3Int.down) return pixelsOffsetDown;
        
        return base.GetPixelOffset(normal);
    }
}