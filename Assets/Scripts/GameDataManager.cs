using UnityEngine;
using System;

namespace MahjongGame
{
    public class GameDataManager
    {
        private static GameDataManager _instance;
        public static GameDataManager Instance => _instance ??= new GameDataManager();

        public int Dice1 { get; private set; } = 1;
        public int Dice2 { get; private set; } = 1;
        public int BankerIndex { get; private set; } = 0;
        public MahjongRegion CurrentRegion { get; private set; } = MahjongRegion.Standard;
        public MahjongRule CurrentRule { get; private set; }
        // 私有构造函数，防止外部实例化
        private GameDataManager() { }

        public void SetDiceValuesFromInput(string inputText)
        {
            string[] nums = inputText.Split(new[] { ' ', ',', ';', '，' }, StringSplitOptions.RemoveEmptyEntries);
            int n1 = 1, n2 = 1;
            if (nums.Length >= 2)
            {
                int.TryParse(nums[0], out n1);
                int.TryParse(nums[1], out n2);
            }

            SetDiceValues(n1, n2);
        }

        public void SetDiceValues(int dice1, int dice2)
        {
            Dice1 = Mathf.Clamp(dice1, 1, 6);
            Dice2 = Mathf.Clamp(dice2, 1, 6);
            BankerIndex = (Dice1 + Dice2 - 1) % 4;
            Debug.Log($"[GameDataManager] Dice: {Dice1}, {Dice2} -> BankerIndex: {BankerIndex}");
        }
        public void SetRegion(MahjongRegion region)
        {
            CurrentRegion = region;

            CurrentRule = MahjongRuleFactory.CreateRule(region);
        }
    }
}