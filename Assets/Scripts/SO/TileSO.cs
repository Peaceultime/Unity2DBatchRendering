using UnityEngine;

[CreateAssetMenu(fileName = "Tile", menuName = "Custom/Tile", order = 0)]
public class TileSO : ScriptableObject
{
    public int spriteIndex;
    public int decoSpriteIndex = -1;
    public static implicit operator Tile(TileSO so)
    {
        return new Tile
        {
            baseSprite = so.spriteIndex,
            decoSprite = so.decoSpriteIndex,
        };
    }
}