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
        [SerializeField] private IMahjongRule gameRule; // Dependency for deck initialization

        public int TileCount => tileDeck.Count;

        /// <summary>
        /// Initializes the DeckManager with the specified game rule.
        /// </summary>
        public void Initialize(IMahjongRule rule)
        {
            gameRule = rule ?? new StandardMahjongRule();
            if (gameRule == null)
            {
                Debug.LogError("GameRule is null in DeckManager. Disabling component.");
                enabled = false;
            }
        }

        /// <summary>
        /// Shuffles the tile deck and initializes it with tiles based on game rules.
        /// </summary>
        public async UniTask<bool> ShuffleTilesAsync(CancellationToken cancellationToken = default)
        {
            if (!enabled)
            {
                Debug.LogError("DeckManager is disabled.");
                return false;
            }

            if (gameRule == null)
            {
                Debug.LogError("GameRule is null. Cannot initialize deck.");
                return false;
            }

            try
            {
                ClearDeck();
                gameRule?.InitializeTileDeck(tileDeck);
                ShuffleTiles();
                await UniTask.Delay(TimeSpan.FromSeconds(MahjongConfig.AnimationDuration), cancellationToken: cancellationToken);
                Debug.Log($"Tiles shuffled! Total tiles: {tileDeck.Count}");
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
            if (!enabled)
            {
                Debug.LogWarning("DeckManager is disabled.");
                return null;
            }

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
            if (!enabled)
            {
                Debug.LogWarning("DeckManager is disabled.");
                return;
            }

            tileDeck.Clear();
        }

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