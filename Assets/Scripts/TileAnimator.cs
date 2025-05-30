using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;

namespace MahjongGame
{
    public static class TileAnimator
    {
        public static float AnimationDuration = 0.5f;
        public static float FlipDuration = 0.25f;

        public static async UniTask AnimateDealAsync(MahjongTile tile, Vector3 targetPos, Quaternion targetRot, CancellationToken cancellationToken = default)
        {
            if (tile?.GameObject == null)
            {
                Debug.LogWarning("Invalid tile or GameObject for AnimateDealAsync.");
                return;
            }

            try
            {
                Transform tileTransform = tile.GameObject.transform;
                var moveTween = tileTransform.DOLocalMove(targetPos, AnimationDuration).SetEase(Ease.OutQuad);
                var rotateTween = tileTransform.DORotateQuaternion(targetRot, AnimationDuration).SetEase(Ease.OutQuad);

                await UniTask.WhenAll(
                    WaitForTweenAsync(moveTween, cancellationToken),
                    WaitForTweenAsync(rotateTween, cancellationToken)
                );
            }
            catch (Exception ex)
            {
                if (!(ex is OperationCanceledException))
                {
                    Debug.LogError($"Failed to animate deal for tile: {ex.Message}");
                }
            }
        }

        public static async UniTask AnimateDrawAsync(MahjongTile tile, Vector3 position, CancellationToken cancellationToken)
        {
            if (tile?.GameObject == null) return;

            MahjongDisplay display = tile.GameObject.GetComponent<MahjongDisplay>();
            if (display != null)
            {
                display.PlayDrawAnimation(position);
                await UniTask.Delay(TimeSpan.FromSeconds(MahjongConfig.AnimationDuration), cancellationToken: cancellationToken);
            }
        }

        public static Sequence CreateDealSequence(MahjongTile tile, Vector3 targetPos, Quaternion targetRot)
        {
            var sequence = DOTween.Sequence();
            if (tile?.GameObject != null)
            {
                sequence.Append(tile.GameObject.transform.DOLocalMove(targetPos, AnimationDuration).SetEase(Ease.OutQuad));
                sequence.Join(tile.GameObject.transform.DORotateQuaternion(targetRot, AnimationDuration).SetEase(Ease.OutQuad));
            }
            else
            {
                Debug.LogWarning("Invalid tile or GameObject for CreateDealSequence.");
            }
            return sequence;
        }

        public static async UniTask AnimateDiscardAsync(MahjongTile tile, Vector3 position, CancellationToken cancellationToken)
        {
            if (tile?.GameObject == null) return;

            MahjongDisplay display = tile.GameObject.GetComponent<MahjongDisplay>();
            if (display != null)
            {
                display.PlayDrawAnimation(position);
                await UniTask.Delay(TimeSpan.FromSeconds(MahjongConfig.AnimationDuration), cancellationToken: cancellationToken);
            }
        }

        private static async UniTask WaitForTweenAsync(Tween tween, CancellationToken cancellationToken)
        {
            if (tween == null) return;

            var completionSource = new UniTaskCompletionSource();
            tween.OnComplete(() => completionSource.TrySetResult());
            tween.OnKill(() => completionSource.TrySetCanceled());

            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                try
                {
                    await completionSource.Task;
                    cts.Token.ThrowIfCancellationRequested();
                }
                catch
                {
                    tween.Kill();
                    throw;
                }
            }
        }

        private static async UniTask WaitForSequenceAsync(Sequence sequence, CancellationToken cancellationToken)
        {
            if (sequence == null) return;

            var completionSource = new UniTaskCompletionSource();
            sequence.OnComplete(() => completionSource.TrySetResult());
            sequence.OnKill(() => completionSource.TrySetCanceled());

            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                try
                {
                    await completionSource.Task;
                    cts.Token.ThrowIfCancellationRequested();
                }
                catch
                {
                    sequence.Kill();
                    throw;
                }
            }
        }
    }
}
