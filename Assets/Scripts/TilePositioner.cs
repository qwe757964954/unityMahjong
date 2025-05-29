using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Threading;

namespace MahjongGame
{
    public static class TilePositioner
    {
        public static void PositionTile(GameObject tile, Transform anchor, int tileIndex, int totalCards, bool reverse = false)
        {
            float spacing = MahjongConfig.TileWidth + MahjongConfig.TileSpacing;
            float totalWidth = (totalCards - 1) * spacing;
            float startOffset = -totalWidth / 2f;

            int index = reverse ?  tileIndex:  (totalCards - 1 - tileIndex);
            float offset = startOffset + index * spacing;

            // 支持任意朝向（根据 anchor.right 排列）
            Vector3 position = anchor.position + anchor.right * offset;

            tile.transform.position = position;
            tile.transform.rotation = anchor.rotation; // 如果你希望统一朝向
            tile.transform.localRotation = Quaternion.Euler(0, 0, 0); // 保持牌面朝上
        }

        
        public static void DrawPositionTile(GameObject tile, Transform anchor, int tileIndex, int totalCards)
        {
            float rowWidth = totalCards * (MahjongConfig.TileWidth + MahjongConfig.TileSpacing) -
                             MahjongConfig.TileSpacing;
            float start = -rowWidth / 2f;
            float pos = start + (totalCards - 1 - tileIndex) * (MahjongConfig.TileWidth + MahjongConfig.TileSpacing) -
                        MahjongConfig.TileWidth;
            tile.transform.position = anchor.position + anchor.right * pos;
            tile.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        }

        public static void PositionDiscardTile(GameObject tile, Transform anchor, int discardIndex)
        {
            const int tilesPerRow = 6; // 每行6张牌
            const int maxRows = 3;     // 最多3行基础行

            float xOffset, zOffset;
            int row, col;

            if (discardIndex < tilesPerRow * maxRows) // 前18张牌 (0-17)
            {
                row = discardIndex / tilesPerRow; // 计算行号 (0,1,2)
                col = discardIndex % tilesPerRow; // 计算列号 (0-5)

                int invCol = (tilesPerRow - 1) - col; // 反转列号（从右到左排列）
                // 居中排列：列索引减去中心偏移量
                xOffset = (invCol - (tilesPerRow / 2.0f - 0.5f)) * 
                          (MahjongConfig.TileWidth + MahjongConfig.TileSpacing);
                zOffset = row * (MahjongConfig.TileHeight + MahjongConfig.TileSpacing);
            }
            else // 额外牌 (discardIndex >= 18)
            {
                // 额外牌全部放在第3行（row = 2），不换行
                row = maxRows - 1; // 固定在第4行（0-based为3）
                col = discardIndex - tilesPerRow * maxRows; // 列号从0开始递增

                // 从-3开始，每列向左递增偏移（单位 = 牌宽+间距）
                xOffset = (-3 - col) * 
                          (MahjongConfig.TileWidth + MahjongConfig.TileSpacing) - (MahjongConfig.TileWidth + MahjongConfig.TileSpacing) / 2;
                zOffset = row * (MahjongConfig.TileHeight + MahjongConfig.TileSpacing);
            }

            // 设置牌的位置
            tile.transform.position = anchor.position
                                      + anchor.right * xOffset
                                      + anchor.forward * zOffset;

            tile.transform.localRotation = Quaternion.Euler(-180, 0, 0); // 设置旋转

            Debug.Log($"Tile {discardIndex}: Row {row}, Col {col}, XOffset {xOffset}, ZOffset {zOffset}");
        }
    }
}