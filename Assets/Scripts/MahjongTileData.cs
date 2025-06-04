using System;

namespace MahjongGame
{
    // 麻将牌类型（扩展花牌）
    public enum MahjongType
    {
        // 基本牌（108张）
        Dot1, Dot2, Dot3, Dot4, Dot5, Dot6, Dot7, Dot8, Dot9,         // 筒子（饼子）
        Bamboo1, Bamboo2, Bamboo3, Bamboo4, Bamboo5, Bamboo6, Bamboo7, Bamboo8, Bamboo9, // 条子（索子）
        Character1, Character2, Character3, Character4, Character5, Character6, Character7, Character8, Character9, // 万子

        // 字牌（28张）
        Wind_East, Wind_South, Wind_West, Wind_North,                   // 风牌（东、南、西、北）
        Dragon_Red, Dragon_Green, Dragon_White,                         // 箭牌（中、发、白）

        // 花牌（8张）
        Flower_Plum, Flower_Orchid, Flower_Bamboo, Flower_Chrysanthemum, // 梅、兰、竹、菊（四君子）
        Season_Spring, Season_Summer, Season_Autumn, Season_Winter       // 春、夏、秋、冬（四季）
    }

    // 游戏状态（不变）
    public enum GameState
    {
        Idle, Shuffling, Dealing, Playing, GameOver
    }

    // 麻将牌数据（更新拼音映射）
    public class MahjongTileData
    {
        public static string GetPinyinForMahjongType(MahjongType type)
        {
            return type switch
            {
                // 筒子（饼子）
                MahjongType.Dot1 => "YiTong",
                MahjongType.Dot2 => "LiangTong",
                MahjongType.Dot3 => "SanTong",
                MahjongType.Dot4 => "SiTong",
                MahjongType.Dot5 => "WuTong",
                MahjongType.Dot6 => "LiuTong",
                MahjongType.Dot7 => "QiTong",
                MahjongType.Dot8 => "BaTong",
                MahjongType.Dot9 => "JiuTong",

                // 条子（索子）
                MahjongType.Bamboo1 => "YiTiao",
                MahjongType.Bamboo2 => "LiangTiao",
                MahjongType.Bamboo3 => "SanTiao",
                MahjongType.Bamboo4 => "SiTiao",
                MahjongType.Bamboo5 => "WuTiao",
                MahjongType.Bamboo6 => "LiuTiao",
                MahjongType.Bamboo7 => "QiTiao",
                MahjongType.Bamboo8 => "BaTiao",
                MahjongType.Bamboo9 => "JiuTiao",

                // 万子
                MahjongType.Character1 => "YiWan",
                MahjongType.Character2 => "LiangWan",
                MahjongType.Character3 => "SaniWan",
                MahjongType.Character4 => "SiWan",
                MahjongType.Character5 => "WuWan",
                MahjongType.Character6 => "LiuWan",
                MahjongType.Character7 => "QiWan",
                MahjongType.Character8 => "BaWan",
                MahjongType.Character9 => "JiuWan",

                // 风牌
                MahjongType.Wind_East => "Dong",
                MahjongType.Wind_South => "Nan",
                MahjongType.Wind_West => "Xi",
                MahjongType.Wind_North => "Bei",

                // 箭牌
                MahjongType.Dragon_Red => "Zhong",
                MahjongType.Dragon_Green => "Fa",
                MahjongType.Dragon_White => "Bai",

                // 花牌（四君子）
                MahjongType.Flower_Plum => "Mei",
                MahjongType.Flower_Orchid => "Lan",
                MahjongType.Flower_Bamboo => "Zhu",
                MahjongType.Flower_Chrysanthemum => "Ju",

                // 花牌（四季）
                MahjongType.Season_Spring => "Spring",
                MahjongType.Season_Summer => "Summer",
                MahjongType.Season_Autumn => "AutumnQiu",
                MahjongType.Season_Winter => "Winter",

                _ => type.ToString() // 默认返回枚举名称
            };
        }
    }
}