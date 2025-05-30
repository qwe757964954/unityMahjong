﻿
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
