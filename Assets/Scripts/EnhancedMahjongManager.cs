using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MahjongGame
{
    public class EnhancedMahjongManager : MonoBehaviour
    {
        [Header("Mahjong Setup")]
        public GameObject mahjongPrefab;
        [SerializeField] private GameObject mahjongTable;

        [Header("Game Rules")]
        public IMahjongRule gameRule = new StandardMahjongRule();

        [Header("Components")]
        public TileAnimator tileAnimator;

        public GameObject MahjongTable
        {
            get => mahjongTable;
            set => mahjongTable = value;
        }

        private EnhancedObjectPool tilePool;
        private List<MahjongTile> activeTiles = new List<MahjongTile>();
        private List<MahjongTile> tileDeck = new List<MahjongTile>();
        private GameState currentState = GameState.Idle;
        private Animator tableAnimator;

        private readonly string[] anchorNames = { "Anchor_Down", "Anchor_Left", "Anchor_Up", "Anchor_Right" };

        private void Awake()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            if (mahjongPrefab == null)
            {
                Debug.LogError("MahjongPrefab is not assigned in EnhancedMahjongManager. Disabling component.");
                enabled = false;
                return;
            }

            if (mahjongTable == null)
            {
                Debug.LogError("MahjongTable is not assigned in EnhancedMahjongManager. Disabling component.");
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

            if (tileAnimator == null)
            {
                tileAnimator = GetComponent<TileAnimator>() ?? gameObject.AddComponent<TileAnimator>();
            }

            if (gameRule == null)
            {
                Debug.LogError("GameRule is not assigned in EnhancedMahjongManager. Disabling component.");
                enabled = false;
                return;
            }
        }

        public async UniTask<bool> InitializeGameAsync(CancellationToken cancellationToken = default)
        {
            if (!enabled)
            {
                Debug.LogError("EnhancedMahjongManager is disabled due to missing or invalid components.");
                return false;
            }

            bool isSuccess = false;
            try
            {
                isSuccess = await InitializeGameSafeAsync(cancellationToken);
                currentState = isSuccess ? GameState.Playing : GameState.Idle;
                if (isSuccess)
                {
                    Debug.Log("Game initialized successfully!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize game: {ex.Message}");
                currentState = GameState.Idle;
            }
            return isSuccess;
        }

        private async UniTask<bool> InitializeGameSafeAsync(CancellationToken cancellationToken)
        {
            try
            {
                currentState = GameState.Shuffling;
                await ShuffleTilesAsync(cancellationToken);
                if (tileDeck.Count == 0)
                {
                    Debug.LogError("No tiles available after shuffling.");
                    return false;
                }

                currentState = GameState.Dealing;
                await DealTilesAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"InitializeGameSafeAsync failed: {ex.Message}");
                return false;
            }
        }

        private async UniTask ShuffleTilesAsync(CancellationToken cancellationToken)
        {
            if (tilePool == null)
            {
                Debug.LogError("TilePool is not initialized. Cannot shuffle tiles.");
                return;
            }

            if (gameRule == null)
            {
                Debug.LogError("GameRule is null. Cannot initialize deck.");
                return;
            }

            ClearTiles();
            gameRule.InitializeDeck(tileDeck);
            ShuffleTiles(tileDeck);
            await UniTask.Delay(TimeSpan.FromSeconds(MahjongConfig.AnimationDuration), cancellationToken: cancellationToken);
            Debug.Log($"Tiles shuffled! Total tiles: {tileDeck.Count}");
        }

        private async UniTask DealTilesAsync(CancellationToken cancellationToken)
        {
            if (tileDeck.Count == 0)
            {
                Debug.LogError("No tiles in deck to deal!");
                return;
            }

            CreateTilesOnRacks();
            await UniTask.Delay(TimeSpan.FromSeconds(MahjongConfig.AnimationDuration), cancellationToken: cancellationToken);
            Debug.Log("Tiles dealt to racks!");
        }

        private void CreateTilesOnRacks()
        {
            Transform tableTransform = mahjongTable?.transform;
            if (tableTransform == null)
            {
                Debug.LogError("Mahjong table not found!");
                return;
            }

            if (gameRule == null)
            {
                Debug.LogError("GameRule is null. Cannot access TilesPerPlayer.");
                return;
            }

            GameObject[] racks = CreateRackOffsets(tableTransform);
            int tilesPerRack = gameRule.TilesPerPlayer;
            int tileIndex = 0;

            for (int rackIndex = 0; rackIndex < 4 && tileIndex < tileDeck.Count; rackIndex++)
            {
                if (racks[rackIndex] == null)
                {
                    Debug.LogError($"Rack {rackIndex} is null. Skipping.");
                    continue;
                }

                for (int i = 0; i < tilesPerRack && tileIndex < tileDeck.Count; i++)
                {
                    CreateTileOnRack(racks[rackIndex], rackIndex, i, tileDeck[tileIndex]);
                    tileIndex++;
                }
            }
        }

        private GameObject[] CreateRackOffsets(Transform tableTransform)
        {
            GameObject[] racks = new GameObject[4];
            bool anyRackCreated = false;

            for (int i = 0; i < 4; i++)
            {
                Transform anchor = tableTransform.Find(anchorNames[i]);
                if (anchor == null)
                {
                    Debug.LogError($"Anchor not found: {anchorNames[i]} on MahjongTable.");
                    continue;
                }

                Transform offset = anchor.Find($"RackOffset_{i}");
                if (offset == null)
                {
                    GameObject rackOffset = new GameObject($"RackOffset_{i}");
                    offset = rackOffset.transform;
                    offset.SetParent(anchor, false);
                    offset.localPosition = MahjongConfig.RackPositions[i];
                }

                racks[i] = offset.gameObject;
                anyRackCreated = true;
            }

            if (!anyRackCreated)
            {
                Debug.LogError("No racks created. Check MahjongTable hierarchy for anchors.");
            }

            return racks;
        }

        private void CreateTileOnRack(GameObject rack, int rackIndex, int tileIndex, MahjongTile tileData)
        {
            if (tilePool == null)
            {
                Debug.LogError("TilePool is not initialized.");
                return;
            }

            if (rack == null)
            {
                Debug.LogError($"Rack {rackIndex} is null. Cannot create tile.");
                return;
            }

            GameObject tileObj = tilePool.Get();
            if (tileObj == null) return;

            tileData.SetGameObject(tileObj);
            tileObj.transform.SetParent(rack.transform, false);

            Vector3 localPos = CalculateTilePosition(rackIndex, tileIndex);
            tileObj.transform.localPosition = localPos;
            tileObj.transform.localRotation = GetTileRotation(rackIndex);

            tileObj.name = $"Mahjong_{rackIndex}_{tileIndex}";
            activeTiles.Add(tileData);
        }

        private Vector3 CalculateTilePosition(int rackIndex, int tileIndex)
        {
            int col = tileIndex / 2;
            int row = 1 - (tileIndex % 2);

            float rowWidth = 17 * (MahjongConfig.TileWidth + MahjongConfig.TileSpacing) - MahjongConfig.TileSpacing;
            float start = -rowWidth / 2f;

            bool reverse = rackIndex == 1 || rackIndex == 2;
            float pos = reverse ? start + (16 - col) * (MahjongConfig.TileWidth + MahjongConfig.TileSpacing)
                                : start + col * (MahjongConfig.TileWidth + MahjongConfig.TileSpacing);

            return (rackIndex == 1 || rackIndex == 3)
                ? new Vector3(0, MahjongConfig.StackHeight * row, pos)
                : new Vector3(pos, MahjongConfig.StackHeight * row, 0);
        }

        private Quaternion GetTileRotation(int rackIndex)
        {
            return rackIndex switch
            {
                1 => Quaternion.Euler(0, 90, 0),
                2 => Quaternion.Euler(0, 180, 0),
                3 => Quaternion.Euler(0, -90, 0),
                _ => Quaternion.identity
            };
        }
        public async UniTask<MahjongTile> DrawTileAsync(int playerIndex, bool isReveal = true, CancellationToken cancellationToken = default)
        {
            if (!enabled)
            {
                Debug.LogError("EnhancedMahjongManager is disabled. Cannot draw tile.");
                return null;
            }

            if (tilePool == null)
            {
                Debug.LogError("TilePool is not initialized. Cannot draw tile.");
                return null;
            }

            if (tileDeck.Count == 0)
            {
                Debug.LogWarning("No more tiles in deck to draw!");
                return null;
            }

            Transform handAnchor = GetHandAnchor(playerIndex, isReveal);
            if (handAnchor == null)
            {
                Debug.LogError($"Hand anchor for player {playerIndex} not found.");
                return null;
            }

            try
            {
                GameObject tileObj = tilePool.Get();
                if (tileObj == null)
                {
                    Debug.LogWarning("Failed to retrieve tile from pool.");
                    return null;
                }

                MahjongTile tile = tileDeck[0];
                tileDeck.RemoveAt(0);

                tile.SetGameObject(tileObj);
                tile.SetParent(handAnchor);

                int tileCount = handAnchor.childCount;
                float totalWidth = tileCount * (MahjongConfig.TileWidth + MahjongConfig.TileSpacing);
                float offset = -totalWidth / 2f + (tileCount * (MahjongConfig.TileWidth + MahjongConfig.TileSpacing)) + (MahjongConfig.TileWidth / 2f);

                Vector3 localPos = (playerIndex == 1 || playerIndex == 3) ?
                    new Vector3(0f, 0f, offset) : new Vector3(offset, 0f, 0f);
                tile.SetLocalPosition(localPos);

                tileObj.transform.localRotation = isReveal ?
                    Quaternion.Euler(-90f, 0f, 0f) : Quaternion.Euler(90f, 0f, 0f);

                if (tileAnimator != null)
                {
                    await tileAnimator.AnimateDrawAsync(tile, localPos, cancellationToken);
                }

                activeTiles.Add(tile);
                Debug.Log($"Drew tile for Player {playerIndex}: {tile.Suit} {tile.Number} at {localPos}");
                return tile;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to draw tile for player {playerIndex}: {ex.Message}");
                return null;
            }
        }

        private Transform GetHandAnchor(int playerIndex, bool isReveal)
        {
            if (mahjongTable == null)
            {
                Debug.LogError("MahjongTable is not assigned.");
                return null;
            }

            string anchorName = anchorNames[playerIndex];
            Transform anchor = mahjongTable.transform.Find(anchorName);
            if (anchor == null)
            {
                Debug.LogError($"Anchor {anchorName} not found on MahjongTable.");
                return null;
            }

            Transform handOffset = anchor.Find($"HandOffset_{playerIndex}");
            if (handOffset == null)
            {
                handOffset = CreateHandOffset(anchor, playerIndex, isReveal);
            }

            return handOffset;
        }
        public async UniTask<bool> DiscardTileAsync(MahjongTile tile, Transform discardAnchor, CancellationToken cancellationToken = default)
        {
            if (!enabled)
            {
                Debug.LogError("EnhancedMahjongManager is disabled. Cannot discard tile.");
                return false;
            }

            if (tile?.GameObject == null || !activeTiles.Contains(tile))
            {
                Debug.LogWarning("Invalid tile to discard!");
                return false;
            }

            try
            {
                tile.SetParent(discardAnchor);
                Vector3 discardPos = Vector3.zero;

                if (tileAnimator != null)
                {
                    await tileAnimator.AnimateDiscardAsync(tile, discardPos, cancellationToken);
                }

                activeTiles.Remove(tile);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to discard tile: {e.Message}");
                return false;
            }
        }

        public async UniTask PlayRackAnimationAsync(CancellationToken cancellationToken = default)
        {
            if (!enabled)
            {
                Debug.LogError("EnhancedMahjongManager is disabled. Cannot play rack animation.");
                return;
            }

            try
            {
                if (tableAnimator != null)
                {
                    tableAnimator.SetFloat("Blend", 1f);
                    await UniTask.WaitUntil(() => !tableAnimator.IsInTransition(0), cancellationToken: cancellationToken);
                }
                else
                {
                    Debug.LogWarning("Table Animator not found!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Animation playback failed: {e.Message}");
            }
        }

        public async UniTask<bool> DealHandCardsByDiceAsync(int dice1, int dice2, CancellationToken cancellationToken)
        {
            if (!enabled || mahjongTable == null)
            {
                Debug.LogError("EnhancedMahjongManager is disabled or MahjongTable is null.");
                return false;
            }

            try
            {
                int startIndex = (dice1 + dice2 - 1) % 4;
                Transform[] anchorTransforms = InitializeHandAnchors(true); // Regular hand offsets
                List<GameObject> allTiles = GetActiveTiles();

                if (allTiles == null || allTiles.Count < 54)
                {
                    Debug.LogError($"Not enough tiles to deal hand cards. Available: {allTiles?.Count ?? 0}");
                    return false;
                }

                int[] playerCardCounts = new int[4];
                int[] handTotals = InitializeHandTotals(startIndex);

                for (int round = 0; round < 3; round++)
                {
                    for (int p = 0; p < 4; p++)
                    {
                        int player = (startIndex + p) % 4;
                        await DealHandCardsAsync(player, 4, handTotals[player], anchorTransforms, allTiles, playerCardCounts, cancellationToken);
                    }
                }

                for (int p = 0; p < 4; p++)
                {
                    int player = (startIndex + p) % 4;
                    await DealHandCardsAsync(player, 1, handTotals[player], anchorTransforms, allTiles, playerCardCounts, cancellationToken);
                }

                await DealHandCardsAsync(startIndex, 1, handTotals[startIndex], anchorTransforms, allTiles, playerCardCounts, cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"DealHandCardsByDiceAsync failed: {ex.Message}");
                return false;
            }
        }

        public async UniTask<bool> RevealHandCardsAsync(CancellationToken cancellationToken)
        {
            if (!enabled || mahjongTable == null)
            {
                Debug.LogError("EnhancedMahjongManager is disabled or MahjongTable is null.");
                return false;
            }

            try
            {
                Transform[] revealAnchors = InitializeHandAnchors(true); // Reveal offsets
                List<GameObject> allTiles = GetActiveTiles();

                if (allTiles == null || allTiles.Count < 54)
                {
                    Debug.LogError($"Not enough tiles to reveal hand cards. Available: {allTiles?.Count ?? 0}");
                    return false;
                }

                int[] playerCardCounts = new int[4];
                int[] handTotals = InitializeHandTotals(0); // Assume Down player as East for simplicity

                for (int player = 0; player < 4; player++)
                {
                    Transform anchor = revealAnchors[player];
                    if (anchor == null) continue;

                    List<GameObject> playerTiles = allTiles.Take(handTotals[player]).ToList();
                    allTiles.RemoveRange(0, playerTiles.Count);

                    await RepositionHandCardsAsync(player, handTotals[player], anchor, playerTiles, playerCardCounts, cancellationToken);
                }

                Debug.Log("Hand cards revealed!");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"RevealHandCardsAsync failed: {ex.Message}");
                return false;
            }
        }

        private Transform[] InitializeHandAnchors(bool isReveal)
        {
            Transform[] anchorTransforms = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                Transform anchor = mahjongTable.transform.Find(anchorNames[i]);
                if (anchor == null)
                {
                    Debug.LogWarning($"Anchor {anchorNames[i]} not found.");
                    continue;
                }

                Transform handOffset = anchor.Find($"HandOffset_{i}") ?? CreateHandOffset(anchor, i, isReveal);
                anchorTransforms[i] = handOffset;
            }
            return anchorTransforms;
        }

        private Transform CreateHandOffset(Transform anchor, int i, bool isReveal)
        {
            Transform rackOffset = anchor.childCount > 0 && anchor.GetChild(0).name == $"RackOffset_{i}" ? anchor.GetChild(0) : anchor;
            Vector3 offsetDirWorld = GetHandOffsetDirection(i, isReveal);
            Vector3 offsetDirLocal = anchor.InverseTransformDirection(offsetDirWorld);
            Transform newHand = new GameObject($"HandOffset_{i}").transform;
            newHand.SetParent(anchor, false);

            Vector3 basePos = rackOffset.localPosition + offsetDirLocal;
            // 使用三元表达式直接赋值，消除冗余代码块
            basePos.y = isReveal && i == 0 ? basePos.y : MahjongConfig.HandOffsetY;
            newHand.localPosition = basePos;

            Quaternion rot = isReveal && i == 0 ? Quaternion.Euler(MahjongConfig.RevealHandRotationX, 0, 0) : GetHandRotation(i);
            newHand.localRotation = rot;

            return newHand;
        }

        private Vector3 GetHandOffsetDirection(int i, bool isReveal)
        {
            if (isReveal && i == 0)
            {
                return MahjongConfig.RevealHandPositionDown;
            }

            return i switch
            {
                0 => new Vector3(0, 0, MahjongConfig.HandOffsetDistance),
                1 => new Vector3(MahjongConfig.HandOffsetDistance, 0, 0),
                2 => new Vector3(0, 0, -MahjongConfig.HandOffsetDistance),
                3 => new Vector3(-MahjongConfig.HandOffsetDistance, 0, 0),
                _ => Vector3.zero
            };
        }

        private Quaternion GetHandRotation(int i)
        {
            return i switch
            {
                1 => Quaternion.Euler(0, 90, 0),
                2 => Quaternion.Euler(0, 180, 0),
                3 => Quaternion.Euler(0, -90, 0),
                _ => Quaternion.identity
            };
        }

        private int[] InitializeHandTotals(int startIndex)
        {
            int[] handTotals = new int[4];
            for (int i = 0; i < 4; i++)
            {
                handTotals[i] = MahjongConfig.InitialHandCount;
            }
            handTotals[startIndex] = MahjongConfig.EastExtraCard;
            return handTotals;
        }

        private async UniTask DealHandCardsAsync(int player, int count, int totalCards, Transform[] anchorTransforms, List<GameObject> handCards, int[] playerCardCounts, CancellationToken cancellationToken)
        {
            if (anchorTransforms[player] == null || playerCardCounts[player] >= totalCards)
            {
                return;
            }

            Transform anchor = anchorTransforms[player];
            float rowWidth = totalCards * (MahjongConfig.TileWidth + MahjongConfig.TileSpacing) - MahjongConfig.TileSpacing;
            float start = -rowWidth / 2f;

            for (int j = 0; j < count && handCards.Count > 0; j++)
            {
                GameObject tile = handCards[0];
                handCards.RemoveAt(0);
                tile.transform.SetParent(anchor);

                float pos = start + (totalCards - 1 - playerCardCounts[player]) * (MahjongConfig.TileWidth + MahjongConfig.TileSpacing);
                tile.transform.position = anchor.position + anchor.right * pos;
                tile.transform.localRotation = Quaternion.Euler(-90, 0, 0);

                playerCardCounts[player]++;
                await UniTask.Delay(TimeSpan.FromSeconds(MahjongConfig.DealAnimationDelay), cancellationToken: cancellationToken);
            }
        }

        private async UniTask RepositionHandCardsAsync(int player, int totalCards, Transform anchor, List<GameObject> playerTiles, int[] playerCardCounts, CancellationToken cancellationToken)
        {
            if (anchor == null || playerCardCounts[player] >= totalCards)
            {
                return;
            }

            float rowWidth = totalCards * (MahjongConfig.TileWidth + MahjongConfig.TileSpacing) - MahjongConfig.TileSpacing;
            float start = -rowWidth / 2f;

            for (int j = 0; j < playerTiles.Count; j++)
            {
                GameObject tile = playerTiles[j];
                tile.transform.SetParent(anchor);

                float pos = start + (totalCards - 1 - playerCardCounts[player]) * (MahjongConfig.TileWidth + MahjongConfig.TileSpacing);
                tile.transform.position = anchor.position + anchor.right * pos;
                tile.transform.localRotation = Quaternion.Euler(-90, 0, 0);

                playerCardCounts[player]++;
                await UniTask.Delay(TimeSpan.FromSeconds(MahjongConfig.DealAnimationDelay), cancellationToken: cancellationToken);
            }
        }

        private void ClearTiles()
        {
            if (tilePool == null)
            {
                Debug.LogError("TilePool is not initialized. Cannot clear tiles.");
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
            tileDeck.Clear();
        }

        public List<GameObject> GetActiveTiles()
        {
            if (!enabled)
            {
                Debug.LogError("EnhancedMahjongManager is disabled. Cannot get active tiles.");
                return new List<GameObject>();
            }

            return activeTiles.Where(t => t?.GameObject != null).Select(t => t.GameObject).ToList();
        }

        public List<MahjongTile> GetActiveMahjongTiles()
        {
            if (!enabled)
            {
                Debug.LogError("EnhancedMahjongManager is disabled. Cannot get active Mahjong tiles.");
                return new List<MahjongTile>();
            }

            return new List<MahjongTile>(activeTiles);
        }

        private void ShuffleTiles(List<MahjongTile> tiles)
        {
            var random = new System.Random();
            for (int i = tiles.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (tiles[i], tiles[j]) = (tiles[j], tiles[i]);
            }
        }
    }
}