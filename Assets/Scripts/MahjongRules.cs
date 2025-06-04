// ========== Mahjong Rules ==========
using System.Collections.Generic;
using UnityEngine;
namespace MahjongGame
{
    // 四川麻将规则
    public class SichuanMahjongRule : MahjongRule
    {
        public override MahjongRegion Region => MahjongRegion.Sichuan;
        public override int TilesPerPlayer => 13;
        public override int TotalTiles => 108;
        public override bool HasFlowers => false;
        
        public override List<MahjongType> ExcludedTiles => new List<MahjongType>
        {
            MahjongType.Wind_East, MahjongType.Wind_South, MahjongType.Wind_West, MahjongType.Wind_North,
            MahjongType.Dragon_Red, MahjongType.Dragon_Green, MahjongType.Dragon_White,
            MahjongType.Flower_Plum, MahjongType.Flower_Orchid, MahjongType.Flower_Bamboo, MahjongType.Flower_Chrysanthemum,
            MahjongType.Season_Spring, MahjongType.Season_Summer, MahjongType.Season_Autumn, MahjongType.Season_Winter
        };

        public override bool IsValidHand(List<MahjongTile> hand)
        {
            // 四川麻将特殊规则：只能碰杠，不能吃
            return hand.Count == TilesPerPlayer;
        }

        public override bool CanWin(List<MahjongTile> hand)
        {
            // 四川麻将特殊胡牌规则（如血战到底）
            return hand.Count == TilesPerPlayer + 1;
        }
        
    }

    // 广东麻将规则
    public class GuangdongMahjongRule : MahjongRule
    {
        public override MahjongRegion Region => MahjongRegion.Guangdong;
        public override int TilesPerPlayer => 34;
        public override int TotalTiles => 136;
        public override bool HasFlowers => true;
        
        public override List<MahjongType> ExcludedTiles => new List<MahjongType>
        {
            MahjongType.Season_Spring, MahjongType.Season_Summer, MahjongType.Season_Autumn, MahjongType.Season_Winter
        };

        public override bool IsValidHand(List<MahjongTile> hand)
        {
            // 广东麻将特殊规则：有花牌
            return hand.Count >= TilesPerPlayer;
        }

        public override bool CanWin(List<MahjongTile> hand)
        {
            // 广东麻将特殊胡牌规则（如鸡胡）
            return hand.Count == TilesPerPlayer + 1;
        }
        
    }

    // 标准麻将规则
    public class StandardMahjongRule : MahjongRule
    {
        public override MahjongRegion Region => MahjongRegion.Standard;
        public override int TilesPerPlayer => 36;
        public override int TotalTiles => 144;
        public override bool HasFlowers => true;
        
        public override List<MahjongType> ExcludedTiles => new List<MahjongType>();

        public override bool IsValidHand(List<MahjongTile> hand)
        {
            return hand.Count == TilesPerPlayer || hand.Count == TilesPerPlayer + 1;
        }

        public override bool CanWin(List<MahjongTile> hand)
        {
            // 标准麻将胡牌规则
            return hand.Count == TilesPerPlayer + 1;
        }
    }

    // 规则工厂
    public static class MahjongRuleFactory
    {
        public static MahjongRule CreateRule(MahjongRegion region)
        {
            return region switch
            {
                MahjongRegion.Sichuan => new SichuanMahjongRule(),
                MahjongRegion.Guangdong => new GuangdongMahjongRule(),
                _ => new StandardMahjongRule()
            };
        }
    }
}
