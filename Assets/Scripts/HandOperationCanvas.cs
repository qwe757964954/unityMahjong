using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace MahjongGame
{
    public class HandOperationCanvas : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Button shuffleButton;
        [SerializeField] private Button sendHandCardButton;
        [SerializeField] private Button drawButton;
        [SerializeField] private Button discardButton;
        [SerializeField] private Transform handAnchor;
        [SerializeField] private Transform discardAnchor;
        [SerializeField] private GameObject riceNumberInput;

        private EnhancedMahjongManager mahjongManager;
        private InputField riceInputField;
        private GameObject selectedTile;

        private void Awake()
        {
            InitializeReferences();
            SetupButtonListeners();
        }

        private void InitializeReferences()
        {
            mahjongManager = FindObjectOfType<EnhancedMahjongManager>();
            riceInputField = riceNumberInput?.GetComponent<InputField>();

            if (shuffleButton == null) Debug.LogError("ShuffleButton is not assigned.");
            if (sendHandCardButton == null) Debug.LogError("SendHandCardButton is not assigned.");
            if (drawButton == null) Debug.LogError("DrawButton is not assigned.");
            if (discardButton == null) Debug.LogError("DiscardButton is not assigned.");
            if (handAnchor == null) Debug.LogError("HandAnchor is not assigned.");
            if (discardAnchor == null) Debug.LogError("DiscardAnchor is not assigned.");
            if (riceNumberInput == null) Debug.LogError("RiceNumberInput is not assigned.");
            if (mahjongManager == null || !mahjongManager.enabled)
            {
                Debug.LogError("EnhancedMahjongManager is not found or is disabled.");
                mahjongManager = null;
            }

            if (shuffleButton == null || sendHandCardButton == null || drawButton == null || discardButton == null ||
                handAnchor == null || discardAnchor == null || riceNumberInput == null || mahjongManager == null)
            {
                Debug.LogError("Missing required components or references in HandOperationCanvas. Disabling component.");
                enabled = false;
            }
        }

        private void SetupButtonListeners()
        {
            if (enabled)
            {
                shuffleButton.onClick.AddListener(() => ShuffleAndSetDiceAsync(this.GetCancellationTokenOnDestroy()).Forget());
                sendHandCardButton.onClick.AddListener(() => DealHandCardsAsync(this.GetCancellationTokenOnDestroy()).Forget());
                drawButton.onClick.AddListener(() => DrawTileAsync(this.GetCancellationTokenOnDestroy()).Forget());
                discardButton.onClick.AddListener(() => DiscardTileAsync(this.GetCancellationTokenOnDestroy()).Forget());
            }
        }

        private async UniTask ShuffleAndSetDiceAsync(CancellationToken cancellationToken)
        {
            if (mahjongManager == null)
            {
                Debug.LogError("MahjongManager is null or disabled.");
                return;
            }

            try
            {
                (int n1, int n2) = ParseDiceInput();
                bool success = await mahjongManager.InitializeGameAsync(cancellationToken);
                if (!success)
                {
                    Debug.LogError($"Failed to initialize game with dice {n1}, {n2}. Check MahjongTable anchors and MahjongConfig.");
                    return;
                }

                await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: cancellationToken);
                await mahjongManager.PlayRackAnimationAsync(cancellationToken);
                await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: cancellationToken);

                Debug.Log($"Shuffled and set dice: {n1}, {n2}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"ShuffleAndSetDiceAsync failed: {ex.Message}");
            }
        }

        private async UniTask DealHandCardsAsync(CancellationToken cancellationToken)
        {
            if (mahjongManager == null)
            {
                Debug.LogError("MahjongManager is null or disabled.");
                return;
            }

            try
            {
                (int n1, int n2) = ParseDiceInput();
                bool success = await mahjongManager.DealHandCardsByDiceAsync(n1, n2, cancellationToken);
                if (success)
                {
                    Debug.Log("Hand cards dealt!");
                }
                else
                {
                    Debug.LogError($"Failed to deal hand cards with dice {n1}, {n2}.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"DealHandCardsAsync failed: {ex.Message}");
            }
        }

        private async UniTask DrawTileAsync(CancellationToken cancellationToken)
        {
            if (mahjongManager == null)
            {
                Debug.LogError("MahjongManager is null or disabled.");
                return;
            }

            try
            {
                MahjongTile tile = await mahjongManager.DrawTileAsync(handAnchor, cancellationToken);
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

        private async UniTask DiscardTileAsync(CancellationToken cancellationToken)
        {
            if (mahjongManager == null)
            {
                Debug.LogError("MahjongManager is null or disabled.");
                return;
            }

            try
            {
                if (selectedTile == null)
                {
                    Debug.LogWarning("No tile selected to discard.");
                    return;
                }

                MahjongTile tile = mahjongManager.GetActiveMahjongTiles().Find(t => t.GameObject == selectedTile);
                if (tile == null)
                {
                    Debug.LogWarning("Selected tile is not a valid MahjongTile.");
                    return;
                }

                bool success = await mahjongManager.DiscardTileAsync(tile, discardAnchor, cancellationToken);
                if (success)
                {
                    Debug.Log($"Discarded tile: {tile.Suit} {tile.Number}");
                    selectedTile = null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"DiscardTileAsync failed: {ex.Message}");
            }
        }

        private (int, int) ParseDiceInput()
        {
            if (riceInputField == null)
            {
                Debug.LogWarning("RiceInputField is null. Using default dice values.");
                return (1, 1);
            }

            string input = riceInputField.text.Trim();
            string[] nums = input.Split(new[] { ' ', ',', ';', '，' }, StringSplitOptions.RemoveEmptyEntries);
            int n1 = 1, n2 = 1;
            if (nums.Length >= 2)
            {
                int.TryParse(nums[0], out n1);
                int.TryParse(nums[1], out n2);
            }
            return (n1, n2);
        }

        public void SelectTile(GameObject tile)
        {
            selectedTile = tile;
        }
    }
}