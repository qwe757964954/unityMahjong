using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Threading;

namespace MahjongGame
{
    /// <summary>
    /// Manages the Mahjong tile deck, including initialization, shuffling, and drawing tiles.
    /// </summary>
    public class DeckManager : MonoBehaviour
    {
        private List<MahjongTile> tileDeck = new List<MahjongTile>();
        private MahjongRule gameRule; // 从GameDataManager获取

        public int TileCount => tileDeck.Count;
        /// <summary>
        /// Shuffles the tile deck and initializes it with tiles based on game rules.
        /// </summary>
        public async UniTask<bool> ShuffleTilesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                gameRule = GameDataManager.Instance.CurrentRule;
                ClearDeck();
                gameRule.InitializeTileDeck(tileDeck);
                ShuffleTiles();
                await UniTask.Delay(TimeSpan.FromSeconds(MahjongConfig.AnimationDuration), 
                    cancellationToken: cancellationToken);
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to shuffle tiles: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Draws the top tile from the deck.
        /// </summary>
        public MahjongTile DrawTile()
        {
            if (!enabled) return null;

            if (tileDeck.Count == 0)
            {
                Debug.LogWarning("No tiles in deck to draw!");
                return null;
            }

            MahjongTile tile = tileDeck[0];
            tileDeck.RemoveAt(0);
            return tile;
        }

        /// <summary>
        /// Clears all tiles from the deck.
        /// </summary>
        public void ClearDeck()
        {
            if (!enabled) return;
            tileDeck.Clear();
        }
        /// <summary>
        /// Fisher-Yates shuffle algorithm for shuffling tiles.
        /// </summary>
        private void ShuffleTiles()
        {
            var random = new System.Random();
            for (int i = tileDeck.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (tileDeck[i], tileDeck[j]) = (tileDeck[j], tileDeck[i]);
            }
        }
    }
}