// ========== Mahjong Rules Interface ==========
using System.Collections.Generic;

namespace MahjongGame
{
    public interface IMahjongRule
    {
        int TilesPerPlayer { get; }
        int TotalTiles { get; }
        void InitializeDeck(List<MahjongTile> deck);
        bool IsValidHand(List<MahjongTile> hand);
        bool CanWin(List<MahjongTile> hand);
    }

    public class StandardMahjongRule : IMahjongRule
    {
        public int TilesPerPlayer => 13;
        public int TotalTiles => 136;

        public void InitializeDeck(List<MahjongTile> deck)
        {
            // Standard 136-tile deck initialization
            deck.Clear();
            
            foreach (MahjongType type in System.Enum.GetValues(typeof(MahjongType)))
            {
                for (int i = 0; i < 4; i++)
                {
                    // Note: GameObject will be set when creating actual tiles
                    deck.Add(new MahjongTile(type, null));
                }
            }
        }

        public bool IsValidHand(List<MahjongTile> hand)
        {
            // Basic validation - should have correct number of tiles
            return hand.Count == TilesPerPlayer || hand.Count == TilesPerPlayer + 1;
        }

        public bool CanWin(List<MahjongTile> hand)
        {
            // Simplified win condition check
            // In a real implementation, this would check for valid melds
            return hand.Count == TilesPerPlayer + 1;
        }
    }
}