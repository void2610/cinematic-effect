using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Void2610.CinematicEffect
{
    /// <summary>
    /// パッケージ内部で使う LitMotion のショートカット拡張。
    /// 利用側プロジェクトの同種拡張とあいまい参照にならないよう internal に留める。
    /// </summary>
    internal static class LitMotionShortcuts
    {
        /// <summary>
        /// Imageの透明度をLitMotionでフェードインさせる
        /// </summary>
        public static MotionHandle FadeIn(this Image image, float duration, Ease ease = Ease.Linear, bool ignoreTimeScale = false)
        {
            return LMotion.Create(image.color.a, 1f, duration)
                .WithEase(ease)
                .WithScheduler(ignoreTimeScale ? MotionScheduler.UpdateIgnoreTimeScale : MotionScheduler.Update)
                .BindToColorA(image)
                .AddTo(image.gameObject);
        }

        /// <summary>
        /// Imageの透明度をLitMotionでフェードアウトさせる
        /// </summary>
        public static MotionHandle FadeOut(this Image image, float duration, Ease ease = Ease.Linear, bool ignoreTimeScale = false)
        {
            return LMotion.Create(image.color.a, 0f, duration)
                .WithEase(ease)
                .WithScheduler(ignoreTimeScale ? MotionScheduler.UpdateIgnoreTimeScale : MotionScheduler.Update)
                .BindToColorA(image)
                .AddTo(image.gameObject);
        }

        /// <summary>
        /// RectTransformのY座標のみをLitMotionで移動
        /// </summary>
        public static MotionHandle MoveToY(this RectTransform rectTransform, float targetY, float duration, Ease ease = Ease.Linear, bool ignoreTimeScale = false)
        {
            return LMotion.Create(rectTransform.anchoredPosition.y, targetY, duration)
                .WithEase(ease)
                .WithScheduler(ignoreTimeScale ? MotionScheduler.UpdateIgnoreTimeScale : MotionScheduler.Update)
                .Bind(y => rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, y))
                .AddTo(rectTransform.gameObject);
        }
    }
}
