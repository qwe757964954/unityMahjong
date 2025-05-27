using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MahjongGame
{
    public class EnhancedMahjongManager : MonoBehaviour
    {
        [Header("Mahjong Setup")]
        [SerializeField]
        private GameObject mahjongPrefab;

        [SerializeField] private GameObject mahjongTable;

        [Header("Game Rules")]
        [SerializeField]
        private IMahjongRule gameRule = new StandardMahjongRule();

        [Header("Components")]
        [SerializeField]
        private TileAnimator tileAnimator;

        [SerializeField] private DeckManager deckManager;
        [SerializeField] private RackManager rackManager;
        [SerializeField] private HandManager handManager;

        public GameObject MahjongTable
        {
            get => mahjongTable;
            set => mahjongTable = value;
        }

        private EnhancedObjectPool tilePool;
        private List<MahjongTile> activeTiles = new List<MahjongTile>();
        private GameState currentState = GameState.Idle;
        private Animator tableAnimator;

        private void Awake()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            if (mahjongPrefab == null || mahjongTable == null || gameRule == null)
            {
                Debug.LogError(
                    "Missing required components (MahjongPrefab, MahjongTable, or GameRule). Disabling EnhancedMahjongManager.");
                enabled = false;
                return;
            }

            try
            {
                tilePool = new EnhancedObjectPool(mahjongPrefab, mahjongTable.transform, MahjongConfig.DefaultPoolSize);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize tilePool: {ex.Message}. Disabling component.");
                enabled = false;
                return;
            }

            tableAnimator = mahjongTable.GetComponent<Animator>();
            tileAnimator = tileAnimator
                ? tileAnimator
                : GetComponent<TileAnimator>() ?? gameObject.AddComponent<TileAnimator>();

            deckManager = GetComponent<DeckManager>() ?? gameObject.AddComponent<DeckManager>();
            deckManager.Initialize(gameRule);

            rackManager = GetComponent<RackManager>() ?? gameObject.AddComponent<RackManager>();
            rackManager.Initialize(mahjongTable);

            handManager = GetComponent<HandManager>() ?? gameObject.AddComponent<HandManager>();
            handManager.Initialize(mahjongTable, tilePool, tileAnimator, deckManager, activeTiles);
        }

        public async UniTask<bool> InitializeGameAsync(CancellationToken cancellationToken = default)
        {
            if (!enabled)
            {
                Debug.LogError("EnhancedMahjongManager is disabled.");
                return false;
            }

            try
            {
                bool success = await InitializeGameSafeAsync(cancellationToken);
                currentState = success ? GameState.Playing : GameState.Idle;
                if (success)
                {
                    Debug.Log("Game initialized successfully!");
                }

                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize game: {ex.Message}");
                currentState = GameState.Idle;
                return false;
            }
        }

        private async UniTask<bool> InitializeGameSafeAsync(CancellationToken cancellationToken)
        {
            currentState = GameState.Shuffling;
            await ShuffleTilesAsync(cancellationToken);
            if (deckManager.TileCount == 0)
            {
                Debug.LogError("No tiles available after shuffling.");
                return false;
            }

            currentState = GameState.Dealing;
            await DealTilesAsync(cancellationToken);
            return true;
        }

        private async UniTask ShuffleTilesAsync(CancellationToken cancellationToken)
        {
            if (deckManager == null || tilePool == null)
            {
                Debug.LogError("DeckManager or TilePool is null.");
                return;
            }

            ClearTiles();
            bool success = await deckManager.ShuffleTilesAsync(cancellationToken);
            if (!success)
            {
                Debug.LogError("Failed to shuffle tiles.");
            }
        }

        private async UniTask DealTilesAsync(CancellationToken cancellationToken)
        {
            if (deckManager?.TileCount == 0 || rackManager == null)
            {
                Debug.LogError("Invalid DeckManager or RackManager state.");
                return;
            }

            CreateTilesOnRacks();
            await UniTask.Delay(TimeSpan.FromSeconds(MahjongConfig.AnimationDuration),
                cancellationToken: cancellationToken);
            Debug.Log("Tiles dealt to racks!");
        }

        private void CreateTilesOnRacks()
        {
            if (gameRule == null || rackManager == null || deckManager == null)
            {
                Debug.LogError("GameRule, RackManager, or DeckManager is null.");
                return;
            }
            
            int tilesPerRack = gameRule.TilesPerPlayer;

            GameObject[] racks = rackManager.CreateRackOffsets();
            int tileIndex = 0;
            for (int rackIndex = 0; rackIndex < 4; rackIndex++)
            {
                if (racks[rackIndex] == null)
                {
                    Debug.LogError($"Rack {rackIndex} is null, skipping.");
                    continue;
                }
                for (int i = 0; i < tilesPerRack; i++)
                {
                    MahjongTile tile = deckManager.DrawTile();
                    if (tile == null)
                    {
                        Debug.LogWarning($"DrawTile returned null at tileIndex {tileIndex} for Rack {rackIndex}");
                        break;
                    }
                    if (rackManager.CreateTileOnRack(racks[rackIndex], rackIndex, i, tile, tilePool))
                    {
                        activeTiles.Add(tile);
                        Debug.Log($"Added tile {tileIndex} to Rack {rackIndex}");
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to create tile {tileIndex} on Rack {rackIndex}");
                    }
                    tileIndex++;
                }
                Debug.Log($"Finished Rack {rackIndex}, tileIndex = {tileIndex}");
            }
        }

        public async UniTask<MahjongTile> DrawTileAsync(int playerIndex, bool isReveal = true,
            CancellationToken cancellationToken = default)
        {
            if (!enabled || handManager == null)
            {
                Debug.LogError("EnhancedMahjongManager or HandManager is disabled/null.");
                return null;
            }

            return await handManager.DrawTileAsync(playerIndex, isReveal, cancellationToken);
        }

        public async UniTask<bool> DiscardTileAsync(MahjongTile tile, Transform discardAnchor,
            CancellationToken cancellationToken = default)
        {
            if (!enabled || handManager == null)
            {
                Debug.LogError("EnhancedMahjongManager or HandManager is disabled/null.");
                return false;
            }

            return await handManager.DiscardTileAsync(tile, discardAnchor, cancellationToken);
        }

        public async UniTask<bool> DealHandCardsByDiceAsync(int dice1, int dice2, CancellationToken cancellationToken)
        {
            if (!enabled || handManager == null)
            {
                Debug.LogError("EnhancedMahjongManager or HandManager is disabled/null.");
                return false;
            }

            return await handManager.DealHandCardsByDiceAsync(dice1, dice2, cancellationToken);
        }

        public async UniTask<bool> RevealHandCardsAsync(CancellationToken cancellationToken)
        {
            if (!enabled || handManager == null)
            {
                Debug.LogError("EnhancedMahjongManager or HandManager is disabled/null.");
                return false;
            }

            return await handManager.RevealHandCardsAsync(cancellationToken);
        }

        public async UniTask PlayRackAnimationAsync(CancellationToken cancellationToken = default)
        {
            if (!enabled)
            {
                Debug.LogError("EnhancedMahjongManager is disabled.");
                return;
            }

            try
            {
                if (tableAnimator != null)
                {
                    tableAnimator.SetFloat("Blend", 1f);
                    await UniTask.WaitUntil(() => !tableAnimator.IsInTransition(0),
                        cancellationToken: cancellationToken);
                }
                else
                {
                    Debug.LogWarning("Table Animator not found!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Animation playback failed: {ex.Message}");
            }
        }

        private void ClearTiles()
        {
            if (tilePool == null || deckManager == null)
            {
                Debug.LogError("TilePool or DeckManager is null.");
                return;
            }

            foreach (var tile in activeTiles)
            {
                if (tile?.GameObject != null)
                {
                    tilePool.Return(tile.GameObject);
                }
            }

            activeTiles.Clear();
            deckManager.ClearDeck();
        }

        public List<GameObject> GetActiveTiles()
        {
            if (!enabled || handManager == null)
            {
                Debug.LogError("EnhancedMahjongManager or HandManager is disabled/null.");
                return new List<GameObject>();
            }

            return handManager.GetActiveTiles();
        }

        public List<MahjongTile> GetActiveMahjongTiles()
        {
            if (!enabled)
            {
                Debug.LogError("EnhancedMahjongManager is disabled.");
                return new List<MahjongTile>();
            }

            return new List<MahjongTile>(activeTiles);
        }
    }
}