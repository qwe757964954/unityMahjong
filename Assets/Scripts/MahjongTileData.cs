using System;

namespace MahjongGame
{
    // 麻将牌类型
    public enum MahjongType
    {
        Dot1, Dot2, Dot3, Dot4, Dot5, Dot6, Dot7, Dot8, Dot9,
        Bamboo1, Bamboo2, Bamboo3, Bamboo4, Bamboo5, Bamboo6, Bamboo7, Bamboo8, Bamboo9,
        Character1, Character2, Character3, Character4, Character5, Character6, Character7, Character8, Character9,
        Wind_East, Wind_South, Wind_West, Wind_North,
        Dragon_Red, Dragon_Green, Dragon_White
    }

    // 游戏状态
    public enum GameState
    {
        Idle, Shuffling, Dealing, Playing, GameOver
    }

    // 麻将牌数据
    public class MahjongTileData
    {
        // 拼音映射方法
        public static string GetPinyinForMahjongType(MahjongType type)
        {
            return type switch
            {
                MahjongType.Dot1 => "YiWan",
                MahjongType.Dot2 => "LiangWan",
                MahjongType.Dot3 => "SaniWan",
                MahjongType.Dot4 => "SiWan",
                MahjongType.Dot5 => "WuWan",
                MahjongType.Dot6 => "LiuWan",
                MahjongType.Dot7 => "QiWan",
                MahjongType.Dot8 => "BaWan",
                MahjongType.Dot9 => "JiuWan",
                MahjongType.Bamboo1 => "YiTiao",
                MahjongType.Bamboo2 => "LiangTiao",
                MahjongType.Bamboo3 => "SanTiao",
                MahjongType.Bamboo4 => "SiTiao",
                MahjongType.Bamboo5 => "WuTiao",
                MahjongType.Bamboo6 => "LiuTiao",
                MahjongType.Bamboo7 => "QiTiao",
                MahjongType.Bamboo8 => "BaTiao",
                MahjongType.Bamboo9 => "JiuTiao",
                MahjongType.Character1 => "YiTong",
                MahjongType.Character2 => "LiangTong",
                MahjongType.Character3 => "SanTong",
                MahjongType.Character4 => "SiTong",
                MahjongType.Character5 => "WuTong",
                MahjongType.Character6 => "LiuTong",
                MahjongType.Character7 => "QiTong",
                MahjongType.Character8 => "BaTong",
                MahjongType.Character9 => "JiuTong",
                MahjongType.Wind_East => "Dong",
                MahjongType.Wind_South => "Nan",
                MahjongType.Wind_West => "Xi",
                MahjongType.Wind_North => "Bei",
                MahjongType.Dragon_Red => "Zhong",
                MahjongType.Dragon_Green => "Fa",
                MahjongType.Dragon_White => "Bai",
                _ => type.ToString() // 默认返回枚举名称
            };
        }
    }
}