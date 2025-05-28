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

        private DeckManager deckManager;
        private RackManager rackManager;
        private HandManager handManager;

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
            handManager.Initialize(mahjongTable, tileAnimator, rackManager);
        }

        public async UniTask<bool> InitializeGameAsync(CancellationToken cancellationToken = default)
        {
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
            ClearTiles();
            bool success = await deckManager.ShuffleTilesAsync(cancellationToken);
            if (!success)
            {
                Debug.LogError("Failed to shuffle tiles.");
            }
        }

        private async UniTask DealTilesAsync(CancellationToken cancellationToken)
        {
            CreateTilesOnRacks();
            await UniTask.Delay(TimeSpan.FromSeconds(MahjongConfig.AnimationDuration),
                cancellationToken: cancellationToken);
        }

        private void CreateTilesOnRacks()
        {
            int tilesPerRack = gameRule.TilesPerPlayer;
            GameObject[] racks = rackManager.CreateRackOffsets();
            int tileIndex = 0;

            int banker = GameDataManager.Instance.BankerIndex;

            for (int p = 0; p < 4; p++)
            {
                int player = (banker + p) % 4;
                for (int i = 0; i < tilesPerRack; i++)
                {
                    MahjongTile tile = deckManager.DrawTile();
                    if (rackManager.CreateTileOnRack(racks[player], player, i, tile, tilePool))
                    {
                        activeTiles.Add(tile);
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to create tile {tileIndex} on Rack {player}");
                    }
                    tileIndex++;
                }
            }
        }


        public async UniTask<MahjongTile> DrawTileAsync(int playerIndex,
            CancellationToken cancellationToken = default)
        {
            return await handManager.DrawTileAsync(playerIndex, cancellationToken);
        }

        public async UniTask<bool> DiscardTileAsync(MahjongTile tile, Transform discardAnchor,
            CancellationToken cancellationToken = default)
        {
            return await handManager.DiscardTileAsync(tile, discardAnchor, cancellationToken);
        }

        public async UniTask<bool> DealHandCardsByDiceAsync( CancellationToken cancellationToken)
        {
            return await handManager.DealHandCardsByDiceAsync(cancellationToken);
        }

        public async UniTask<bool> RevealHandCardsAsync(CancellationToken cancellationToken)
        {
            return await handManager.RevealHandCardsAsync(cancellationToken);
        }

        public async UniTask PlayRackAnimationAsync(CancellationToken cancellationToken = default)
        {
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
        
        public List<MahjongTile> GetActiveMahjongTiles()
        {
            return new List<MahjongTile>(activeTiles);
        }
    }
}