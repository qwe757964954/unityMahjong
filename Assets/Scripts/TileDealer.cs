using UnityEngine; using Cysharp.Threading.Tasks; using System.Collections.Generic; using DG.Tweening; using System.Threading;

namespace MahjongGame
{
    public class TileDealer
    {
        private RackManager rackManager;

        public TileDealer(RackManager rack)
        {
            rackManager = rack;
        }

        public List<Transform> DealTiles(PlayerHandState handState, int count, CancellationToken cancellationToken,
            bool isBankerExtraTile = false)
        {
            List<Transform> newTiles = new List<Transform>();
            if (handState.Anchor == null || handState.CurrentCardCount >= handState.TotalCards)
                return newTiles;

            Transform anchor = handState.Anchor;
            bool isSelfReveal =
                (anchor == handState.Anchor &&
                 handState.Anchor.name == "HandSelfPlaying"); // Assuming HandSelfPlaying is named
            bool reverse = isSelfReveal;

            for (int j = 0; j < count; j++)
            {
                MahjongTile tile = rackManager.DrawTileFromRack();
                if (tile == null)
                {
                    Debug.LogWarning($"No more tiles to draw.");
                    break;
                }

                GameObject tileObj = tile.GameObject;
                tileObj.transform.SetParent(anchor, false);

                int layer = isSelfReveal ? LayerMask.NameToLayer("PlayerHandLayer") : LayerMask.NameToLayer("Default");
                LayerUtil.SetLayerRecursively(tileObj, layer);

                // 为庄家最后一张牌添加额外的半张牌间隔
                float extraOffset = (isBankerExtraTile && j == count - 1) ? (-MahjongConfig.TileWidth / 2f) : 0f;
                Debug.Log($"isBankerExtraTile:{isBankerExtraTile},,j:${j},count${count},extraOffset${extraOffset}");
                TilePositioner.PositionTile(tileObj, anchor, handState.CurrentCardCount, handState.TotalCards,
                    extraOffset);

                newTiles.Add(tileObj.transform);

                handState.CurrentCardCount++;
            }

            return newTiles;
        }

        public async UniTask FlipTilesAsync(List<Transform> tiles)
        {
            var flipTasks = new List<UniTask>();
            foreach (var tile in tiles)
            {
                var flipTween = tile.DOLocalRotate(new Vector3(-90, 0, 0), 0.25f);
                flipTasks.Add(flipTween.ToUniTask());
            }

            await UniTask.WhenAll(flipTasks);
        }
    }
}