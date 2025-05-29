using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using DG.Tweening;
using System.Threading;

namespace MahjongGame
{
    public class HandManager : MonoBehaviour
    {
        [SerializeField] private TileAnimator tileAnimator;

        [Header("HandSelfPlaying")] [SerializeField]
        private Transform HandSelfPlaying;

        [Header("Anchor Transforms (Down, Left, Up, Right)")] [SerializeField]
        private Transform[] anchorTransforms = new Transform[4];

        private RackManager rackManager;

        public void Initialize(GameObject table, TileAnimator animator, RackManager rack)
        {
            tileAnimator = animator;
            rackManager = rack;
            if (rackManager == null)
            {
                Debug.LogError("Required dependencies not assigned in HandManager. Disabling component.");
                enabled = false;
            }
        }

        public async UniTask<MahjongTile> DrawTileAsync(int playerIndex,
            CancellationToken cancellationToken = default)
        {
            try
            {
                Transform handAnchor = GetHandAnchor(playerIndex, true);
                int handIndex = handAnchor.childCount;
                int totalCards = handIndex + 1;

                MahjongTile tile = await DrawAndPlaceTileAsync(playerIndex, handAnchor, handAnchor.childCount,
                    totalCards: totalCards, cancellationToken);

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

            // if (tile == null || tile.GameObject == null || !activeTiles.Contains(tile))
            // {
            //     Debug.LogWarning(
            //         $"Invalid tile to discard. Tile: {(tile == null ? "null" : tile.ToString())}, ActiveTiles Count: {activeTiles.Count}");
            //     return false;
            // }
            //
            // if (discardAnchor == null)
            // {
            //     Debug.LogWarning("Discard anchor is null.");
            //     return false;
            // }

            try
            {
                tile.SetParent(discardAnchor);
                Vector3 discardPos = Vector3.zero;

                if (tileAnimator != null)
                {
                    await tileAnimator.AnimateDiscardAsync(tile, discardPos, cancellationToken);
                }

                // bool removed = activeTiles.Remove(tile);
                // if (!removed)
                // {
                //     Debug.LogWarning($"Failed to remove tile {tile.Suit} {tile.Number} from activeTiles.");
                // }

                Debug.Log($"Discarded tile: {tile.Suit} {tile.Number}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to discard tile {tile.Suit} {tile.Number}: {ex.Message}");
                return false;
            }
        }

        public async UniTask<bool> DealHandCardsByDiceAsync(CancellationToken cancellationToken)
        {
            Transform[] anchorTransforms = InitializeHandAnchors(true);
            int[] playerCardCounts = new int[4];
            int banker = GameDataManager.Instance.BankerIndex;

            int[] handTotals = InitializeHandTotals(banker);

            for (int round = 0; round < 3; round++)
            {
                for (int p = 0; p < 4; p++)
                {
                    int player = (banker + p) % 4;
                    await DealHandCardsAsync(player, 4, handTotals[player], anchorTransforms, playerCardCounts,
                        cancellationToken);
                }
            }

            for (int p = 0; p < 4; p++)
            {
                int player = (banker + p) % 4;
                await DealHandCardsAsync(player, 1, handTotals[player], anchorTransforms, playerCardCounts,
                    cancellationToken);
            }

            await DealHandCardsAsync(banker, 1, handTotals[banker], anchorTransforms, playerCardCounts,
                cancellationToken);
            await UniTask.WhenAll(
                Enumerable.Range(0, 4).Select(p =>
                {
                    int player = (banker + p) % 4;
                    return RevealHandAsync(anchorTransforms[player]);
                })
            );

            return true;
        }
        private async UniTask RevealHandAsync(Transform anchor)
        {
            // 第一步：所有牌同时转向正面 (0, 0, 0)
            var resetTasks = new List<UniTask>();
            foreach (Transform tile in anchor)
            {
                Tweener tween = tile.DOLocalRotate(Vector3.zero, 0.1f); // 快速归位
                resetTasks.Add(tween.ToUniTask());
            }
            await UniTask.WhenAll(resetTasks);

            // 第二步：短暂停顿，制造动画节奏
            await UniTask.Delay(100); // 100ms，可调

            // 第三步：所有牌同时翻开 (-90, 0, 0)
            var flipTasks = new List<UniTask>();
            foreach (Transform tile in anchor)
            {
                Tweener tween = tile.DOLocalRotate(new Vector3(-90, 0, 0), 0.25f);
                flipTasks.Add(tween.ToUniTask());
            }
            await UniTask.WhenAll(flipTasks);
        }

        private async UniTask<MahjongTile> DrawAndPlaceTileAsync(
            int playerIndex,
            Transform anchor,
            int handIndex,
            int totalCards,
            CancellationToken cancellationToken)
        {
            MahjongTile tile = rackManager.DrawTileFromRack();
            if (tile == null)
            {
                Debug.LogWarning($"No more tiles to draw for player {playerIndex}.");
                return null;
            }

            GameObject tileObj = tile.GameObject;
            tileObj.transform.SetParent(anchor);

            // 定位
            TilePositioner.DrawPositionTile(tileObj, anchor, handIndex, totalCards);
            return tile;
        }
        
        private async UniTask DealHandCardsAsync(int player, int count, int totalCards,
            Transform[] anchorTransforms, int[] playerCardCounts, CancellationToken cancellationToken)
        {
            if (anchorTransforms[player] == null || playerCardCounts[player] >= totalCards)
                return;

            Transform anchor = anchorTransforms[player];
            List<UniTask> flipTasks = new List<UniTask>();

            for (int j = 0; j < count; j++)
            {
                MahjongTile tile = rackManager.DrawTileFromRack();
                if (tile == null)
                {
                    Debug.LogWarning($"No more tiles to draw for player {player}.");
                    break;
                }

                GameObject tileObj = tile.GameObject;
                tileObj.transform.SetParent(anchor);

                // 定位
                TilePositioner.PositionTile(tileObj, anchor, playerCardCounts[player], totalCards);

                // 启动翻转动画，但不 await，收集任务
                var flipTween = tileObj.transform.DOLocalRotate(new Vector3(-90, 0, 0), 0.25f);
                flipTasks.Add(flipTween.ToUniTask());
                playerCardCounts[player]++;
            }
            // 等待所有翻转动画完成
            await UniTask.WhenAll(flipTasks);
        }
        public async UniTask<bool> RevealHandCardsAsync(CancellationToken cancellationToken)
        {
            return true;
        }

        public Transform GetHandAnchor(int playerIndex, bool isReveal)
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
    }
}