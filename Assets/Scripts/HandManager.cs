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

        [Header("HandSelfPlaying")]
        [SerializeField]
        private Transform HandSelfPlaying;

        [Header("Anchor Transforms (Down, Left, Up, Right)")]
        [SerializeField]
        private Transform[] anchorTransforms = new Transform[4];

        private RackManager rackManager;

        public void Initialize(GameObject table, RackManager rack)
        {
            rackManager = rack;
            if (rackManager == null)
            {
                Debug.LogError("Required dependencies not assigned in HandManager. Disabling component.");
                enabled = false;
            }
        }

        public async UniTask<MahjongTile> DrawTileAsync(int playerIndex, CancellationToken cancellationToken = default)
        {
            try
            {
                Transform handAnchor = GetHandAnchor(playerIndex, true);
                int handIndex = handAnchor.childCount;
                int totalCards = handIndex + 1;

                MahjongTile tile = await DrawAndPlaceTileAsync(playerIndex, handAnchor, handIndex, totalCards, cancellationToken);

                return tile;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to draw tile for player {playerIndex}: {ex.Message}");
                return null;
            }
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
            tileObj.transform.SetParent(anchor, false);

            int layer = (playerIndex == 0 && anchor == HandSelfPlaying)
                ? LayerMask.NameToLayer("PlayerHandLayer")
                : LayerMask.NameToLayer("Default");
            LayerUtil.SetLayerRecursively(tileObj, layer);

            bool isOrtho = (playerIndex == 0);
            bool isSelfPlayer = (playerIndex == 0 && anchor == HandSelfPlaying);
            TilePositioner.DrawPositionTile(tileObj, anchor, handIndex, totalCards, isOrtho, isSelfPlayer);

            return tile;
        }
        public async UniTask<bool> DealHandCardsByDiceAsync(CancellationToken cancellationToken)
        {
            Transform[] anchors = InitializeHandAnchors(true);
            int[] playerCardCounts = new int[4];
            int banker = GameDataManager.Instance.BankerIndex;
            int[] handTotals = InitializeHandTotals(banker);

            for (int round = 0; round < 3; round++)
            {
                for (int p = 0; p < 4; p++)
                {
                    int player = (banker + p) % 4;
                    await DealHandCardsAsync(player, 4, handTotals[player], anchors, playerCardCounts, cancellationToken);
                }
            }

            for (int p = 0; p < 4; p++)
            {
                int player = (banker + p) % 4;
                await DealHandCardsAsync(player, 1, handTotals[player], anchors, playerCardCounts, cancellationToken);
            }

            await DealHandCardsAsync(banker, 1, handTotals[banker], anchors, playerCardCounts, cancellationToken);

            await UniTask.WhenAll(Enumerable.Range(0, 4).Select(p => RevealHandAsync(anchors[(banker + p) % 4])));

            return true;
        }
        private async UniTask DealHandCardsAsync(
            int player,
            int count,
            int totalCards,
            Transform[] anchors,
            int[] playerCardCounts,
            CancellationToken cancellationToken)
        {
            if (anchors[player] == null || playerCardCounts[player] >= totalCards)
                return;

            Transform anchor = anchors[player];
            bool isSelfReveal = (player == 0 && anchor == HandSelfPlaying);
            bool isOrtho = (player == 0);
            bool reverse = isSelfReveal;

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
                tileObj.transform.SetParent(anchor, false);

                int layer = isSelfReveal ? LayerMask.NameToLayer("PlayerHandLayer") : LayerMask.NameToLayer("Default");
                LayerUtil.SetLayerRecursively(tileObj, layer);

                TilePositioner.PositionTile(tileObj, anchor, playerCardCounts[player], totalCards, reverse);

                var flipTween = tileObj.transform.DOLocalRotate(new Vector3(-90, 0, 0), 0.25f);
                flipTasks.Add(flipTween.ToUniTask());

                playerCardCounts[player]++;
            }

            await UniTask.WhenAll(flipTasks);
        }

        private async UniTask RevealHandAsync(Transform anchor)
        {
            var resetTasks = new List<UniTask>();
            foreach (Transform tile in anchor)
            {
                resetTasks.Add(tile.DOLocalRotate(Vector3.zero, 0.1f).ToUniTask());
            }
            await UniTask.WhenAll(resetTasks);
            await UniTask.Delay(100);

            var flipTasks = new List<UniTask>();
            foreach (Transform tile in anchor)
            {
                flipTasks.Add(tile.DOLocalRotate(new Vector3(-90, 0, 0), 0.25f).ToUniTask());
            }
            await UniTask.WhenAll(flipTasks);
        }
        public MahjongTile GetHandTile(int playerIndex, bool isReveal, int indexFromEnd = 0)
        {
            Transform handAnchor = GetHandAnchor(playerIndex, isReveal);
            if (handAnchor == null || handAnchor.childCount == 0) return null;

            int targetIndex = Mathf.Clamp(handAnchor.childCount - 1 - indexFromEnd, 0, handAnchor.childCount - 1);
            Transform tileTransform = handAnchor.GetChild(targetIndex);
            MahjongDisplay display = tileTransform.GetComponent<MahjongDisplay>();
            return display?.TileData;
        }
        public List<MahjongTile> GetLastNHandTiles(int playerIndex, bool isReveal, int count)
        {
            List<MahjongTile> result = new List<MahjongTile>();
            Transform handAnchor = GetHandAnchor(playerIndex, isReveal);

            if (handAnchor == null || handAnchor.childCount == 0)
            {
                Debug.LogWarning($"No tiles in hand for player {playerIndex}.");
                return result;
            }

            int startIndex = Mathf.Max(handAnchor.childCount - count, 0);

            for (int i = startIndex; i < handAnchor.childCount; i++)
            {
                Transform tileTransform = handAnchor.GetChild(i);
                MahjongDisplay display = tileTransform.GetComponent<MahjongDisplay>();
                if (display != null && display.TileData != null)
                {
                    result.Add(display.TileData);
                }
                else
                {
                    Debug.LogWarning($"Missing MahjongDisplay or TileData on hand tile at index {i}.");
                }
            }

            return result;
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