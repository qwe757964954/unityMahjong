
// ========== Game Controller (MVC Pattern) ==========
using UnityEngine;
using System.Collections;
using Cysharp.Threading.Tasks; // For UniTask
using System.Threading;
using System;
namespace MahjongGame
{
    public class GameController : MonoBehaviour
    {
        [Header("References")]
        public EnhancedMahjongManager mahjongManager;
        public HandOperationCanvas handCanvas;
        public TileAnimator tileAnimator;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip drawSound;
        public AudioClip discardSound;
        public AudioClip shuffleSound;

        private void Start()
        {
            ValidateReferences();
        }

        private void ValidateReferences()
        {
            if (mahjongManager == null)
                mahjongManager = FindObjectOfType<EnhancedMahjongManager>();
            if (handCanvas == null)
                handCanvas = FindObjectOfType<HandOperationCanvas>();
            if (tileAnimator == null)
                tileAnimator = GetComponent<TileAnimator>() ?? gameObject.AddComponent<TileAnimator>();
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        /// <summary>
        /// Starts the game with the specified start index.
        /// </summary>
        public async UniTask StartGameAsync(int startIndex = 0, CancellationToken cancellationToken = default)
        {
            try
            {
                PlaySound(shuffleSound);
                bool success = await mahjongManager.InitializeGameAsync(cancellationToken);
                if (success)
                {
                    Debug.Log("Game started successfully!");
                    // Optionally trigger additional initialization, e.g., handCanvas setup
                }
                else
                {
                    Debug.LogError("Failed to initialize game.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"StartGameAsync failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Draws a tile and plays the draw sound.
        /// </summary>
        private async UniTask DrawTileAsync(CancellationToken cancellationToken)
        {
            if (mahjongManager == null)
            {
                Debug.LogError("MahjongManager is null or disabled.");
                return;
            }

            try
            {
                int playerIndex = 0; // Down player; adjust based on game logic
                MahjongTile tile = await mahjongManager.DrawTileAsync(playerIndex, cancellationToken);
                if (tile != null)
                {
                    Debug.Log($"Drew tile: {tile.Suit} {tile.Number}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"DrawTileAsync failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Discards a tile and plays the discard sound.
        /// </summary>
        public async UniTask DiscardTileAsync(MahjongTile tile, Transform discardAnchor, CancellationToken cancellationToken = default)
        {
            try
            {
                bool success = await mahjongManager.DiscardTileAsync(tile, discardAnchor, cancellationToken);
                if (success)
                {
                    PlaySound(discardSound);
                    Debug.Log($"Discarded tile: {tile.Suit} {tile.Number}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to discard tile: {ex.Message}");
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        public void AddHapticFeedback()
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}
