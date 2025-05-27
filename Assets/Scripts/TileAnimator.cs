// ========== Tile Animation Manager ==========
using UnityEngine;
using DG.Tweening;
using System.Collections;

namespace MahjongGame
{
    public class TileAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        public float animationDuration = 0.5f;
        public float flipDuration = 0.25f;
        
        public void AnimateDeal(MahjongTile tile, Vector3 targetPos, Quaternion targetRot)
        {
            if (tile?.GameObject != null)
            {
                tile.GameObject.transform.DOLocalMove(targetPos, animationDuration).SetEase(Ease.OutQuad);
                tile.GameObject.transform.DORotateQuaternion(targetRot, animationDuration).SetEase(Ease.OutQuad);
            }
        }

        public void AnimateDraw(MahjongTile tile, Vector3 targetPos)
        {
            if (tile?.GameObject == null) return;
            
            var sequence = DOTween.Sequence();
            sequence.Append(tile.GameObject.transform.DORotate(new Vector3(0, 0, 180), flipDuration))
                .OnComplete(() => tile.GameObject.transform.DOLocalMove(targetPos, flipDuration));
        }

        public Sequence CreateDealSequence(MahjongTile tile, Vector3 targetPos, Quaternion targetRot)
        {
            var sequence = DOTween.Sequence();
            if (tile?.GameObject != null)
            {
                sequence.Append(tile.GameObject.transform.DOLocalMove(targetPos, animationDuration));
                sequence.Join(tile.GameObject.transform.DORotateQuaternion(targetRot, animationDuration));
            }
            return sequence;
        }

        public void AnimateDiscard(MahjongTile tile, Vector3 targetPos)
        {
            if (tile?.GameObject != null)
            {
                tile.GameObject.transform.DOMove(targetPos, animationDuration).SetEase(Ease.OutBounce);
            }
        }
    }
}