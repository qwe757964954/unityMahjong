using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MahjongGame
{
    public class HandManager : MonoBehaviour
    {
        [SerializeField] private GameObject mahjongTable;
        [SerializeField] private TileAnimator tileAnimator;
        private EnhancedObjectPool tilePool;
        private DeckManager deckManager;
        private List<MahjongTile> activeTiles;
        private readonly string[] anchorNames = { "Anchor_Down", "Anchor_Left", "Anchor_Up", "Anchor_Right" };

        public void Initialize(GameObject table, EnhancedObjectPool pool, TileAnimator animator, DeckManager deck,
            List<MahjongTile> tiles)
        {
            mahjongTable = table;
            tilePool = pool;
            tileAnimator = animator;
            deckManager = deck;
            activeTiles = tiles ?? new List<MahjongTile>();

            if (mahjongTable == null || tilePool == null || deckManager == null || activeTiles == null)
            {
                Debug.LogError("Required dependencies not assigned in HandManager. Disabling component.");
                enabled = false;
            }
        }

        public async UniTask<MahjongTile> DrawTileAsync(int playerIndex, bool isReveal = true,
            CancellationToken cancellationToken = default)
        {
            if (!enabled)
            {
                Debug.LogError("HandManager is disabled. Cannot draw tile.");
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

                MahjongTile tile = deckManager.DrawTile();
                if (tile == null)
                {
                    Debug.LogWarning("No tile drawn from deck.");
                    tilePool.Return(tileObj);
                    return null;
                }

                tile.SetGameObject(tileObj);
                tileObj.transform.SetParent(handAnchor);

                int currentHandCount = handAnchor.childCount;
                int totalCards = currentHandCount + 1;
                float rowWidth = totalCards * (MahjongConfig.TileWidth + MahjongConfig.TileSpacing) -
                                 MahjongConfig.TileSpacing;
                float start = -rowWidth / 2f;
                float pos = start + (totalCards - 1 - currentHandCount) *
                    (MahjongConfig.TileWidth + MahjongConfig.TileSpacing);
                TilePositioner.PositionTile(tileObj, handAnchor, currentHandCount, totalCards);

                if (!isReveal)
                {
                    tileObj.transform.localRotation = Quaternion.Euler(90, 0, 0);
                }

                if (tileAnimator != null)
                {
                    Vector3 localPos = handAnchor.InverseTransformPoint(tileObj.transform.position);
                    await tileAnimator.AnimateDrawAsync(tile, localPos, cancellationToken);
                }

                await UniTask.Delay(TimeSpan.FromSeconds(MahjongConfig.DealAnimationDelay),
                    cancellationToken: cancellationToken);

                activeTiles.Add(tile);
                Debug.Log(
                    $"Drew tile for Player {playerIndex}: {tile.Suit} {tile.Number} at position {tileObj.transform.position.x}, currentHandCount={currentHandCount}, totalCards={totalCards}, pos={pos}");
                return tile;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to draw tile for player {playerIndex}: {ex.Message}");
                return null;
            }
        }

        public async UniTask<bool> DiscardTileAsync(MahjongTile tile, Transform discardAnchor,
            CancellationToken cancellationToken = default)
        {
            if (!enabled)
            {
                Debug.LogError("HandManager is disabled. Cannot discard tile.");
                return false;
            }

            if (tile == null || tile.GameObject == null || !activeTiles.Contains(tile))
            {
                Debug.LogWarning(
                    $"Invalid tile to discard. Tile: {(tile == null ? "null" : tile.ToString())}, ActiveTiles Count: {activeTiles.Count}");
                return false;
            }

            if (discardAnchor == null)
            {
                Debug.LogWarning("Discard anchor is null.");
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

                bool removed = activeTiles.Remove(tile);
                if (!removed)
                {
                    Debug.LogWarning($"Failed to remove tile {tile.Suit} {tile.Number} from activeTiles.");
                }

                Debug.Log($"Discarded tile: {tile.Suit} {tile.Number}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to discard tile {tile.Suit} {tile.Number}: {ex.Message}");
                return false;
            }
        }

        public async UniTask<bool> DealHandCardsByDiceAsync(int dice1, int dice2, CancellationToken cancellationToken)
        {
            if (!enabled || mahjongTable == null)
            {
                Debug.LogError("HandManager is disabled or MahjongTable is null.");
                return false;
            }

            try
            {
                int startIndex = (dice1 + dice2 - 1) % 4;
                Transform[] anchorTransforms = InitializeHandAnchors(true);
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
                        await DealHandCardsAsync(player, 4, handTotals[player], anchorTransforms, allTiles,
                            playerCardCounts, cancellationToken);
                    }
                }

                for (int p = 0; p < 4; p++)
                {
                    int player = (startIndex + p) % 4;
                    await DealHandCardsAsync(player, 1, handTotals[player], anchorTransforms, allTiles,
                        playerCardCounts, cancellationToken);
                }

                await DealHandCardsAsync(startIndex, 1, handTotals[startIndex], anchorTransforms, allTiles,
                    playerCardCounts, cancellationToken);

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
                Debug.LogError("HandManager is disabled or MahjongTable is null.");
                return false;
            }

            try
            {
                Transform[] revealAnchors = InitializeHandAnchors(true);
                List<GameObject> allTiles = GetActiveTiles();

                if (allTiles == null || allTiles.Count < 54)
                {
                    Debug.LogError($"Not enough tiles to reveal hand cards. Available: {allTiles?.Count ?? 0}");
                    return false;
                }

                int[] playerCardCounts = new int[4];
                int[] handTotals = InitializeHandTotals(0);

                for (int player = 0; player < 4; player++)
                {
                    Transform anchor = revealAnchors[player];
                    if (anchor == null) continue;

                    List<GameObject> playerTiles = allTiles.Take(handTotals[player]).ToList();
                    allTiles.RemoveRange(0, playerTiles.Count);

                    await RepositionHandCardsAsync(player, handTotals[player], anchor, playerTiles, playerCardCounts,
                        cancellationToken);
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

        private Transform CreateHandOffset(Transform anchor, int playerIndex, bool isReveal)
        {
            Transform rackOffset = anchor.childCount > 0 && anchor.GetChild(0).name == $"RackOffset_{playerIndex}"
                ? anchor.GetChild(0)
                : anchor;
            Vector3 offsetDirWorld = GetHandOffsetDirection(playerIndex, isReveal);
            Vector3 offsetDirLocal = anchor.InverseTransformDirection(offsetDirWorld);
            Transform newHand = new GameObject($"HandOffset_{playerIndex}").transform;
            newHand.SetParent(anchor, false);

            Vector3 basePos = rackOffset.localPosition + offsetDirLocal;
            basePos.y = isReveal && playerIndex == 0 ? basePos.y : MahjongConfig.HandOffsetY;
            newHand.localPosition = basePos;

            Quaternion rot = isReveal && playerIndex == 0
                ? Quaternion.Euler(MahjongConfig.RevealHandRotationX, 0, 0)
                : GetHandRotation(playerIndex);
            newHand.localRotation = rot;

            return newHand;
        }

        private Vector3 GetHandOffsetDirection(int playerIndex, bool isReveal)
        {
            if (isReveal && playerIndex == 0)
            {
                return MahjongConfig.RevealHandPositionDown;
            }

            return playerIndex switch
            {
                0 => new Vector3(0, 0, MahjongConfig.HandOffsetDistance),
                1 => new Vector3(MahjongConfig.HandOffsetDistance, 0, 0),
                2 => new Vector3(0, 0, -MahjongConfig.HandOffsetDistance),
                3 => new Vector3(-MahjongConfig.HandOffsetDistance, 0, 0),
                _ => Vector3.zero
            };
        }

        private Quaternion GetHandRotation(int playerIndex)
        {
            return playerIndex switch
            {
                1 => Quaternion.Euler(0, 90, 0),
                2 => Quaternion.Euler(0, 180, 0),
                3 => Quaternion.Euler(0, -90, 0),
                _ => Quaternion.identity
            };
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

        private async UniTask DealHandCardsAsync(int player, int count, int totalCards, Transform[] anchorTransforms,
            List<GameObject> handCards, int[] playerCardCounts, CancellationToken cancellationToken)
        {
            if (anchorTransforms[player] == null || playerCardCounts[player] >= totalCards)
            {
                return;
            }

            Transform anchor = anchorTransforms[player];

            for (int j = 0; j < count && handCards.Count > 0; j++)
            {
                GameObject tile = handCards[0];
                handCards.RemoveAt(0);
                tile.transform.SetParent(anchor);

                TilePositioner.PositionTile(tile, anchor, playerCardCounts[player], totalCards);

                playerCardCounts[player]++;
                await UniTask.Delay(TimeSpan.FromSeconds(MahjongConfig.DealAnimationDelay),
                    cancellationToken: cancellationToken);
            }
        }

        private async UniTask RepositionHandCardsAsync(int player, int totalCards, Transform anchor,
            List<GameObject> playerTiles, int[] playerCardCounts, CancellationToken cancellationToken)
        {
            if (anchor == null || playerCardCounts[player] >= totalCards)
            {
                return;
            }

            for (int j = 0; j < playerTiles.Count; j++)
            {
                GameObject tile = playerTiles[j];
                tile.transform.SetParent(anchor);

                TilePositioner.PositionTile(tile, anchor, playerCardCounts[player], totalCards);

                playerCardCounts[player]++;
                await UniTask.Delay(TimeSpan.FromSeconds(MahjongConfig.DealAnimationDelay),
                    cancellationToken: cancellationToken);
            }
        }

        public List<GameObject> GetActiveTiles()
        {
            if (!enabled)
            {
                Debug.LogError("HandManager is disabled. Cannot get active tiles.");
                return new List<GameObject>();
            }

            return activeTiles.Where(t => t?.GameObject != null).Select(t => t.GameObject).ToList();
        }
    }
}