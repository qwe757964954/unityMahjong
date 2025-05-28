using System;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using UnityEngine; // Added this line for Component

namespace Cysharp.Threading.Tasks
{
    public static class DOTweenAsyncExtensions
    {
        public static UniTask ToUniTask(this Tween tween, TweenCancelBehaviour cancelBehaviour = TweenCancelBehaviour.Kill)
        {
            if (tween == null || !tween.IsActive() || !tween.IsPlaying())
            {
                return UniTask.CompletedTask;
            }

            // Check if the tween's target is a Component and if its GameObject is active
            if (tween.target is Component component && component != null && !component.gameObject.activeInHierarchy)
            {
                return UniTask.CompletedTask;
            }

            var tcs = new UniTaskCompletionSource();

            tween.OnComplete(() => tcs.TrySetResult());
            tween.OnKill(() =>
            {
                if (cancelBehaviour == TweenCancelBehaviour.Kill)
                {
                    tcs.TrySetCanceled();
                }
            });

            return tcs.Task;
        }

        public static UniTask ToUniTask(this Tween tween, IProgress<float> progress, TweenCancelBehaviour cancelBehaviour = TweenCancelBehaviour.Kill)
        {
            if (tween == null || !tween.IsActive() || !tween.IsPlaying())
            {
                return UniTask.CompletedTask;
            }

            // Check if the tween's target is a Component and if its GameObject is active
            if (tween.target is Component component && component != null && !component.gameObject.activeInHierarchy)
            {
                return UniTask.CompletedTask;
            }

            var tcs = new UniTaskCompletionSource();

            tween.OnUpdate(() => progress.Report(tween.ElapsedPercentage()));
            tween.OnComplete(() => tcs.TrySetResult());
            tween.OnKill(() =>
            {
                if (cancelBehaviour == TweenCancelBehaviour.Kill)
                {
                    tcs.TrySetCanceled();
                }
            });

            return tcs.Task;
        }

        public enum TweenCancelBehaviour
        {
            Kill,
            Complete
        }
    }
}