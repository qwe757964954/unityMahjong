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
        [Header("HandSelfPlaying")]
        [SerializeField] private Transform HandSelfPlaying;
        [Header("Anchor Transforms (Down, Left, Up, Right)")]
        [SerializeField] private Transform[] anchorTransforms = new Transform[4];
        

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
            // ✅ 特殊情况：玩家 0 且是明牌阶段，使用 HandSelfPlaying
            if (playerIndex == 0 && isReveal)
            {
                return HandSelfPlaying;
            }
            // 通用处理：使用 anchorTransforms
            if (playerIndex < 0 || playerIndex >= anchorTransforms.Length)
            {
                Debug.LogError($"Invalid playerIndex: {playerIndex}");
                return null;
            }

            Transform anchor = anchorTransforms[playerIndex];
            return anchor;
        }
        

        private Transform[] InitializeHandAnchors(bool isReveal)
        {
            Transform[] anchors = new Transform[4];

            for (int i = 0; i < 4; i++)
            {
                if (i == 0 && isReveal)
                {
                    anchors[0] = HandSelfPlaying;
                }
                else
                {
                    anchors[i] = anchorTransforms[i];
                }
                
            }

            return anchors;
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
            return activeTiles.Where(t => t?.GameObject != null).Select(t => t.GameObject).ToList();
        }
    }
}