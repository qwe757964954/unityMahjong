using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Threading;
namespace MahjongGame
{
    public static class TilePositioner
    {
        public static void PositionTile(GameObject tile, Transform anchor, int tileIndex, int totalCards)
        {
            float rowWidth = totalCards * (MahjongConfig.TileWidth + MahjongConfig.TileSpacing) - MahjongConfig.TileSpacing;
            float start = -rowWidth / 2f;
            float pos = start + (totalCards - 1 - tileIndex) * (MahjongConfig.TileWidth + MahjongConfig.TileSpacing);
            tile.transform.position = anchor.position + anchor.right * pos;
            tile.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
        
        public static void DrawPositionTile(GameObject tile, Transform anchor, int tileIndex, int totalCards)
        {
            float rowWidth = totalCards * (MahjongConfig.TileWidth + MahjongConfig.TileSpacing) - MahjongConfig.TileSpacing;
            float start = -rowWidth / 2f;
            float pos = start + (totalCards - 1 - tileIndex) * (MahjongConfig.TileWidth + MahjongConfig.TileSpacing) - MahjongConfig.TileWidth;
            tile.transform.position = anchor.position + anchor.right * pos;
            tile.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        }
    }
}