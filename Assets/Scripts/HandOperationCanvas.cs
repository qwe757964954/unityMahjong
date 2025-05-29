using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace MahjongGame
{
    public class HandOperationCanvas : MonoBehaviour
    {
        [Header("UI Elements")] [SerializeField]
        private Button shuffleButton;

        [SerializeField] private Button sendHandCardButton;
        [SerializeField] private Button drawButton;
        [SerializeField] private Button discardButton;
        [SerializeField] private Button revealHandButton;
        [SerializeField] private Transform discardAnchor;
        [SerializeField] private InputField riceInputField;
        [SerializeField] private InputField drawPlayerIndexInputField;
        [SerializeField] private InputField discardPlayerIndexInputField;

        private EnhancedMahjongManager mahjongManager;
        private GameObject selectedTile;

        private void Start()
        {
            InitializeReferences();
            SetupButtonListeners();
        }

        private void InitializeReferences()
        {
            mahjongManager = FindObjectOfType<EnhancedMahjongManager>();
            Debug.Log(
                $"InitializeReferences GameDataManager {GameDataManager.Instance} riceInputField{riceInputField}");
            GameDataManager.Instance.SetDiceValuesFromInput(riceInputField.text);
        }

        private void SetupButtonListeners()
        {
            if (enabled)
            {
                shuffleButton.onClick.AddListener(() =>
                    ShuffleAndSetDiceAsync(this.GetCancellationTokenOnDestroy()).Forget());
                sendHandCardButton.onClick.AddListener(() =>
                    DealHandCardsAsync(this.GetCancellationTokenOnDestroy()).Forget());
                drawButton.onClick.AddListener(() => DrawTileAsync(this.GetCancellationTokenOnDestroy()).Forget());
                discardButton.onClick.AddListener(() =>
                    DiscardTileAsync(this.GetCancellationTokenOnDestroy()).Forget());
                revealHandButton.onClick.AddListener(() =>
                    RevealHandCardsAsync(this.GetCancellationTokenOnDestroy()).Forget());
            }
        }

        private async UniTask ShuffleAndSetDiceAsync(CancellationToken cancellationToken)
        {
            bool success = await mahjongManager.InitializeGameAsync(cancellationToken);
            if (!success)
            {
                Debug.LogError(
                    $"Failed to initialize game with dice. Check MahjongTable anchors and MahjongConfig.");
                return;
            }

            await mahjongManager.PlayRackAnimationAsync(cancellationToken);
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: cancellationToken);
        }

        private async UniTask DealHandCardsAsync(CancellationToken cancellationToken)
        {
            bool success = await mahjongManager.DealHandCardsByDiceAsync(cancellationToken);
            if (success)
            {
                Debug.Log("Hand cards dealt!");
            }
            else
            {
                Debug.LogError($"Failed to deal hand cards with dice");
            }
        }

        private async UniTask DrawTileAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Parse player index from input
                int playerIndex = ParsePlayerIndex();
                MahjongTile tile = await mahjongManager.DrawTileAsync(playerIndex, cancellationToken);
                if (tile != null)
                {
                    Debug.Log($"Drew tile for Player {playerIndex}: {tile.Suit} {tile.Number}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"DrawTileAsync failed: {ex.Message}");
            }
        }

        private int ParsePlayerIndex()
        {
            if (drawPlayerIndexInputField == null || string.IsNullOrWhiteSpace(drawPlayerIndexInputField.text))
            {
                Debug.LogWarning("PlayerIndexInput is null or empty. Defaulting to player 0.");
                return 0;
            }

            string input = drawPlayerIndexInputField.text.Trim();
            if (int.TryParse(input, out int playerIndex))
            {
                // Ensure playerIndex is valid (0–3)
                if (playerIndex >= 0 && playerIndex <= 3)
                {
                    return playerIndex;
                }

                Debug.LogWarning($"Invalid player index: {playerIndex}. Must be 0–3. Defaulting to 0.");
            }
            else
            {
                Debug.LogWarning($"Failed to parse player index: {input}. Defaulting to player 0.");
            }

            return 0;
        }

        private async UniTask DiscardTileAsync(CancellationToken cancellationToken)
        {
            // Validate and parse player index from input field
            if (discardPlayerIndexInputField == null || string.IsNullOrEmpty(discardPlayerIndexInputField.text))
            {
                Debug.LogWarning("Discard player index input field is not assigned or empty.");
                return;
            }

            if (!int.TryParse(discardPlayerIndexInputField.text, out int playerIndex) || playerIndex < 0 || playerIndex >= 4)
            {
                Debug.LogWarning($"Invalid player index: {discardPlayerIndexInputField.text}. Must be between 0 and 3.");
                return;
            }

            // Get the last tile from the player's hand
            MahjongTile tile = mahjongManager.GetLastHandTile(playerIndex);
            if (tile == null)
            {
                Debug.LogWarning($"No valid tile to discard for player {playerIndex}.");
                return;
            }
            
            // Discard the tile
            bool success = await mahjongManager.DiscardTileAsync(tile, playerIndex, cancellationToken);
            if (success)
            {
                Debug.Log($"Discarded tile: {tile.Suit} {tile.Number} for player {playerIndex}");
                selectedTile = null; // Clear selected tile if used elsewhere
            }
        }

        private async UniTask RevealHandCardsAsync(CancellationToken cancellationToken)
        {
            if (mahjongManager == null)
            {
                Debug.LogError("MahjongManager is null or disabled.");
                return;
            }

            try
            {
                bool success = await mahjongManager.RevealHandCardsAsync(cancellationToken);
                if (success)
                {
                    Debug.Log("Hand cards revealed!");
                }
                else
                {
                    Debug.LogError("Failed to reveal hand cards.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"RevealHandCardsAsync failed: {ex.Message}");
            }
        }

        public void SelectTile(GameObject tile)
        {
            selectedTile = tile;
        }
    }
}