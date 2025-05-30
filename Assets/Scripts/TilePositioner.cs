// TilePositioner.cs
using UnityEngine;

namespace MahjongGame
{
    public static class TilePositioner
    {
        public static void PositionTile(GameObject tile, Transform anchor, int tileIndex, int totalCards, bool reverse = false)
        {
            float spacing = MahjongConfig.TileWidth + MahjongConfig.TileSpacing;
            float totalWidth = (totalCards - 1) * spacing;
            float startOffset = -totalWidth / 2f;

            int index = reverse ? tileIndex : (totalCards - 1 - tileIndex);
            float offset = startOffset + index * spacing;

            Vector3 position = anchor.TransformPoint(Vector3.right * offset);

            tile.transform.position = position;
            tile.transform.rotation = anchor.rotation;
            tile.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }

        public static void DrawPositionTile(GameObject tile, Transform anchor, int tileIndex, int totalCards, bool isOrtho = false, bool isSelfPlayer = false, bool reverse = false)
        {
            float spacing = MahjongConfig.TileWidth + MahjongConfig.TileSpacing;
            float rowWidth = (totalCards - 1) * spacing;
            float start = -rowWidth / 2f;
            int index = reverse ? tileIndex : (totalCards - 1 - tileIndex);
            float offset = start + index * spacing;

            Vector3 position = anchor.position + anchor.right * offset;

            if (isOrtho && isSelfPlayer)
            {
                tile.transform.position = anchor.TransformPoint(Vector3.right * offset);
                tile.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                tile.transform.localScale = Vector3.one;
            }
            else
            {
                // 其他玩家抓牌时再偏移一个宽度
                position -= anchor.right * spacing;
                tile.transform.position = position;
                tile.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                tile.transform.localScale = MahjongConfig.PerspectiveTileScale;
            }
        }

        public static void PositionDiscardTile(GameObject tile, Transform anchor, int discardIndex)
        {
            const int tilesPerRow = 6;
            const int maxRows = 3;

            float xOffset, zOffset;
            int row, col;

            if (discardIndex < tilesPerRow * maxRows)
            {
                row = discardIndex / tilesPerRow;
                col = discardIndex % tilesPerRow;

                int invCol = (tilesPerRow - 1) - col;
                xOffset = (invCol - (tilesPerRow / 2.0f - 0.5f)) * (MahjongConfig.TileWidth + MahjongConfig.TileSpacing);
                zOffset = row * (MahjongConfig.TileHeight + MahjongConfig.TileSpacing);
            }
            else
            {
                row = maxRows - 1;
                col = discardIndex - tilesPerRow * maxRows;

                xOffset = (-3 - col) * (MahjongConfig.TileWidth + MahjongConfig.TileSpacing)
                          - (MahjongConfig.TileWidth + MahjongConfig.TileSpacing) / 2;
                zOffset = row * (MahjongConfig.TileHeight + MahjongConfig.TileSpacing);
            }

            tile.transform.position = anchor.position
                                      + (anchor.rotation * Vector3.right) * xOffset * anchor.localScale.y
                                      + (anchor.rotation * Vector3.forward) * zOffset * anchor.localScale.z;
            tile.transform.localRotation = Quaternion.Euler(-180, 0, 0);
        }
    }
}