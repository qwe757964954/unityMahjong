using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using System.Collections.Generic;

namespace MahjongGame
{
    public class HandOperationCanvas : MonoBehaviour
    {
        [Header("UI Elements")] [SerializeField]
        private Button shuffleButton;

        [SerializeField] private Button sendHandCardButton;
        [SerializeField] private Button drawButton;
        [SerializeField] private Button discardButton;
        [SerializeField] private Button RefreshButton;
        [SerializeField] private Button revealHandButton;
        [SerializeField] private Button chouPungButton;
        [SerializeField] private Button ExposedKongButton;
        [SerializeField] private Button ConcealedKongButton;
        [SerializeField] private Button SupplementKongButton;
        [SerializeField] private Button winButton;

        [SerializeField] private InputField riceInputField;
        [SerializeField] private InputField drawPlayerIndexInputField;
        [SerializeField] private InputField discardPlayerIndexInputField;
        [SerializeField] private InputField actionPlayerIndexInputField;

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
                drawButton.onClick.AddListener(DrawTileAsync);
                discardButton.onClick.AddListener(() =>
                    DiscardTileAsync(this.GetCancellationTokenOnDestroy()).Forget());
                revealHandButton.onClick.AddListener(() =>
                    RevealHandCardsAsync(this.GetCancellationTokenOnDestroy()).Forget());
                chouPungButton.onClick.AddListener(ChouActionAsync);
                RefreshButton.onClick.AddListener(RefreshActionAsync);
                ExposedKongButton.onClick.AddListener(() =>
                    PungActionAsync(this.GetCancellationTokenOnDestroy()).Forget());
                ConcealedKongButton.onClick.AddListener(() =>
                    KongActionAsync(this.GetCancellationTokenOnDestroy()).Forget());
                SupplementKongButton.onClick.AddListener(() =>
                    KongActionAsync(this.GetCancellationTokenOnDestroy()).Forget());
                winButton.onClick.AddListener(() =>
                    WinActionAsync(this.GetCancellationTokenOnDestroy()).Forget());
            }
        }

        private void ChouActionAsync()
        {
            string[] nums = actionPlayerIndexInputField.text.Split(new[] { ' ', ',', ';', '，' },
                StringSplitOptions.RemoveEmptyEntries);
            int n1 = 1, n2 = 1;
            if (nums.Length >= 2)
            {
                int.TryParse(nums[0], out n1);
                int.TryParse(nums[1], out n2);
            }

            List<MahjongTile> tiles = mahjongManager.GetLastTwoHandTiles(n1);
            MahjongTile targetTile = mahjongManager.GetLastDiscardTile(n2);
            mahjongManager.PlaceChowPungKong(n1, n2, tiles, targetTile);
        }

        private void RefreshActionAsync()
        {
            // Parse player index from input
            int playerIndex = ParsePlayerIndex();
            mahjongManager.RefreshHandPositions(playerIndex, true);
        }

        private async UniTask PungActionAsync(CancellationToken cancellationToken)
        {
        }

        private async UniTask KongActionAsync(CancellationToken cancellationToken)
        {
        }

        private async UniTask WinActionAsync(CancellationToken cancellationToken)
        {
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

            mahjongManager.PlayRackAnimation();
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

        private void DrawTileAsync()
        {
            // Parse player index from input
            int playerIndex = ParsePlayerIndex();
            MahjongTile tile = mahjongManager.DrawTileAsync(playerIndex);
            if (tile != null)
            {
                Debug.Log($"Drew tile for Player {playerIndex}: {tile.Suit} {tile.Number}");
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

            if (!int.TryParse(discardPlayerIndexInputField.text, out int playerIndex) || playerIndex < 0 ||
                playerIndex >= 4)
            {
                Debug.LogWarning(
                    $"Invalid player index: {discardPlayerIndexInputField.text}. Must be between 0 and 3.");
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