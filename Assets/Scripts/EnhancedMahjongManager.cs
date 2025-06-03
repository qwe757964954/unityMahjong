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
        
        [SerializeField]
        private MahjongTableTimeline tableTimeline;
        [Header("Manager References")]
        [SerializeField]
        private DiceController diceController;
        [SerializeField]
        private HandManager handManager;
        [SerializeField]
        private AreaManager areaManager;
        [SerializeField]
        private DiscardManager discardManager;
        private DeckManager deckManager;
        private RackManager rackManager;
        public GameObject MahjongTable
        {
            get => mahjongTable;
            set => mahjongTable = value;
        }

        private EnhancedObjectPool tilePool;
        private List<MahjongTile> activeTiles = new List<MahjongTile>();
        private GameState currentState = GameState.Idle;

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
            
            deckManager = GetComponent<DeckManager>() ?? gameObject.AddComponent<DeckManager>();
            deckManager.Initialize(gameRule);
            rackManager = GetComponent<RackManager>() ?? gameObject.AddComponent<RackManager>();
            rackManager.Initialize(mahjongTable);
            handManager.Initialize(mahjongTable, rackManager);
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


        public MahjongTile DrawTileAsync(int playerIndex)
        {
            MahjongTile tile = rackManager.DrawTileFromRack();
            if (tile == null)
            {
                Debug.LogWarning($"No more tiles to draw for player {playerIndex}.");
                return null;
            }
            handManager.DrawTileAsync(playerIndex,tile);
            return tile;
        }

        public async UniTask<bool> DiscardTileAsync(MahjongTile tile, int playerIndex,
            CancellationToken cancellationToken = default)
        {
            return await discardManager.DiscardTileAsync(tile, playerIndex, cancellationToken);
        }

        public async UniTask<bool> DealHandCardsByDiceAsync(CancellationToken cancellationToken)
        {
            return await handManager.DealHandCardsByDiceAsync(cancellationToken);
        }

        public async UniTask<bool> RevealHandCardsAsync(CancellationToken cancellationToken)
        {
            // return await handManager.RevealHandCardsAsync(cancellationToken);
            return true;
        }
        public void PlaceChowPungKong(int operatingPlayerIndex, int targetPlayerIndex, List<MahjongTile> tiles,
            MahjongTile targetTile = null)
        {
            areaManager.PlaceChowPungKong(operatingPlayerIndex,targetPlayerIndex, tiles, targetTile);
        }
        public void PlaceConcealedKong(int playerIndex, List<MahjongTile> tiles)
        {
            areaManager.PlaceConcealedKong(playerIndex, tiles);
        }
        public void SupplementKong(int playerIndex, int groupIndex, MahjongTile tileToAdd)
        {
            areaManager.SupplementKong(playerIndex, groupIndex,tileToAdd);
        }
        public void PlayRackAnimation()
        {
            diceController.SetDiceNumbers(GameDataManager.Instance.Dice1,GameDataManager.Instance.Dice2);
            tableTimeline.ResetAndPlayTimeline();
        }

        public MahjongTile GetLastDiscardTile(int playerIndex)
        {
            return discardManager.GetDiscardTile(playerIndex);
        }

        public MahjongTile GetLastHandTile(int playerIndex)
        {
            return handManager.GetHandTile(playerIndex, true);
        }
        public List<MahjongTile> GetLastFourHandTiles(int playerIndex)
        {
            return handManager.GetLastNHandTiles(playerIndex, true, 4);
        }
        public List<MahjongTile> GetLastThreeHandTiles(int playerIndex)
        {
            return handManager.GetLastNHandTiles(playerIndex, true, 3);
        }
        public List<MahjongTile> GetLastTwoHandTiles(int playerIndex)
        {
            return handManager.GetLastNHandTiles(playerIndex, true, 2);
        }

        public void RefreshHandPositions(int playerIndex, bool isReveal)
        {
            handManager.RefreshHandPositions(playerIndex, isReveal);
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