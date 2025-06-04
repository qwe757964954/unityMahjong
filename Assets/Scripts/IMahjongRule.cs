// ========== Mahjong Rules Interface ==========
using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    // 麻将地区类型
    public enum MahjongRegion
    {
        Sichuan,    // 四川麻将
        Guangdong,  // 广东麻将
        Standard     // 标准麻将
    }

    // 麻将规则基类
    public abstract class MahjongRule
    {
        public abstract MahjongRegion Region { get; }
        public abstract int TilesPerPlayer { get; }
        public abstract int TotalTiles { get; }
        public abstract List<MahjongType> ExcludedTiles { get; }
        public abstract bool HasFlowers { get; }

        public virtual void InitializeTileDeck(List<MahjongTile> deck)
        {
            deck.Clear();

            foreach (MahjongType type in System.Enum.GetValues(typeof(MahjongType)))
            {
                if (ExcludedTiles.Contains(type)) continue;

                int count = (type >= MahjongType.Flower_Plum && !HasFlowers) ? 0 :
                            (type >= MahjongType.Flower_Plum) ? 1 : 4;

                for (int i = 0; i < count; i++)
                {
                    deck.Add(new MahjongTile(type, null));
                }
            }
        }

        public abstract bool IsValidHand(List<MahjongTile> hand);
        public abstract bool CanWin(List<MahjongTile> hand);

        public virtual Vector3 CalculateTilePosition(int rackIndex, int tileIndex, int totalTiles)
        {
            int col = tileIndex / 2;
            int row = 1 - (tileIndex % 2); // row: 0 for top, 1 for bottom
            int totalRows = (totalTiles) / 2; // 每个玩家的牌数除以2，向上取整
            float spacing = MahjongConfig.TileWidth + MahjongConfig.TileSpacing;
            float start = - (spacing * (totalRows - 1)) / 2f;

            bool reverse = rackIndex == 1 || rackIndex == 2;
            float pos = reverse
                ? start + (totalRows - 1 - col) * spacing
                : start + col * spacing;

            float height = MahjongConfig.StackHeight * row;

            return (rackIndex == 1 || rackIndex == 3)
                ? new Vector3(0, height, pos)
                : new Vector3(pos, height, 0);
        }
        
        public virtual Quaternion GetTileRotation(int rackIndex)
        {
            Quaternion rotation = rackIndex switch
            {
                1 => Quaternion.Euler(0, 90, 0),
                2 => Quaternion.Euler(0, 180, 0),
                3 => Quaternion.Euler(0, -90, 0),
                _ => Quaternion.identity
            };
            return rotation;
        }
    }
}
