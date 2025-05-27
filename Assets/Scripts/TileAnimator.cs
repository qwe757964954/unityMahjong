using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System; // For OperationCanceledException
using System.Threading;

namespace MahjongGame
{
    public class TileAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        public float animationDuration = 0.5f;
        public float flipDuration = 0.25f;

        /// <summary>
        /// Animates a tile being dealt to a target position and rotation.
        /// </summary>
        public async UniTask AnimateDealAsync(MahjongTile tile, Vector3 targetPos, Quaternion targetRot, CancellationToken cancellationToken = default)
        {
            if (tile?.GameObject == null)
            {
                Debug.LogWarning("Invalid tile or GameObject for AnimateDealAsync.");
                return;
            }

            try
            {
                Transform tileTransform = tile.GameObject.transform;
                var moveTween = tileTransform.DOLocalMove(targetPos, animationDuration).SetEase(Ease.OutQuad);
                var rotateTween = tileTransform.DORotateQuaternion(targetRot, animationDuration).SetEase(Ease.OutQuad);

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

        /// <summary>
        /// Animates a tile being drawn with a flip and move sequence.
        /// </summary>
        public async UniTask AnimateDrawAsync(MahjongTile tile, Vector3 position, CancellationToken cancellationToken)
        {
            if (tile?.GameObject == null) return;

            MahjongDisplay display = tile.GameObject.GetComponent<MahjongDisplay>();
            if (display != null)
            {
                display.PlayDrawAnimation(position);
                await UniTask.Delay(System.TimeSpan.FromSeconds(MahjongConfig.AnimationDuration), cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Creates a deal animation sequence (non-async, for compatibility with existing code).
        /// </summary>
        public Sequence CreateDealSequence(MahjongTile tile, Vector3 targetPos, Quaternion targetRot)
        {
            var sequence = DOTween.Sequence();
            if (tile?.GameObject != null)
            {
                sequence.Append(tile.GameObject.transform.DOLocalMove(targetPos, animationDuration).SetEase(Ease.OutQuad));
                sequence.Join(tile.GameObject.transform.DORotateQuaternion(targetRot, animationDuration).SetEase(Ease.OutQuad));
            }
            else
            {
                Debug.LogWarning("Invalid tile or GameObject for CreateDealSequence.");
            }
            return sequence;
        }

        /// <summary>
        /// Animates a tile being discarded to a target position.
        /// </summary>
        public async UniTask AnimateDiscardAsync(MahjongTile tile, Vector3 position, CancellationToken cancellationToken)
        {
            if (tile?.GameObject == null) return;

            MahjongDisplay display = tile.GameObject.GetComponent<MahjongDisplay>();
            if (display != null)
            {
                display.PlayDrawAnimation(position); // Reuse for discard, or customize
                await UniTask.Delay(System.TimeSpan.FromSeconds(MahjongConfig.AnimationDuration), cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Helper method to wait for a DOTween Tween to complete with cancellation support.
        /// </summary>
        private async UniTask WaitForTweenAsync(Tween tween, CancellationToken cancellationToken)
        {
            if (tween == null)
            {
                return;
            }

            var completionSource = new UniTaskCompletionSource();
            tween.OnComplete(() => completionSource.TrySetResult());
            tween.OnKill(() => completionSource.TrySetCanceled());

            // Fallback: Manual cancellation check
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                try
                {
                    await completionSource.Task; // Wait for tween completion
                    cts.Token.ThrowIfCancellationRequested(); // Check for cancellation
                }
                catch
                {
                    tween.Kill(); // Clean up tween on cancellation
                    throw;
                }
            }
        }

        /// <summary>
        /// Helper method to wait for a DOTween Sequence to complete with cancellation support.
        /// </summary>
        private async UniTask WaitForSequenceAsync(Sequence sequence, CancellationToken cancellationToken)
        {
            if (sequence == null)
            {
                return;
            }

            var completionSource = new UniTaskCompletionSource();
            sequence.OnComplete(() => completionSource.TrySetResult());
            sequence.OnKill(() => completionSource.TrySetCanceled());

            // Fallback: Manual cancellation check
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                try
                {
                    await completionSource.Task; // Wait for sequence completion
                    cts.Token.ThrowIfCancellationRequested(); // Check for cancellation
                }
                catch
                {
                    sequence.Kill(); // Clean up sequence on cancellation
                    throw;
                }
            }
        }
    }
}