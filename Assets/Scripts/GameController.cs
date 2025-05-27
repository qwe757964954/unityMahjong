
// ========== Game Controller (MVC Pattern) ==========
using UnityEngine;
using System.Collections;

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

        public void StartGame(int startIndex = 0)
        {
            StartCoroutine(StartGameRoutine(startIndex));
        }

        private IEnumerator StartGameRoutine(int startIndex)
        {
            PlaySound(shuffleSound);
            yield return StartCoroutine(mahjongManager.InitializeGameRoutine());
            
            if (handCanvas != null)
            {
                handCanvas.UpdateUI();
            }
        }

        public void DrawTile(Transform handAnchor)
        {
            var tile = mahjongManager.DrawTile(handAnchor);
            if (tile != null)
            {
                PlaySound(drawSound);
                Debug.Log($"Drew tile: {tile.Suit} {tile.Number}");
            }
        }

        public void DiscardTile(MahjongTile tile, Transform discardAnchor)
        {
            if (mahjongManager.DiscardTile(tile, discardAnchor))
            {
                PlaySound(discardSound);
                Debug.Log($"Discarded tile: {tile.Suit} {tile.Number}");
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
