using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine.UI;

namespace Void2610.CinematicEffect
{
    /// <summary>
    /// 全画面オーバーレイをフェードイン → ホールド → フェードアウトさせるフラッシュ演出。
    /// <see cref="ImageFlashConfig.Sprite"/> を指定すると任意のスプライト画像を全画面表示し、
    /// 未指定なら <see cref="ImageFlashConfig.TintColor"/> のソリッドカラーで塗る。
    /// </summary>
    public sealed class ImageFlashEffect : ConfigurableCinematicEffectBase<ImageFlashConfig>
    {
        public override string EffectName => "フラッシュ";

        // null なら再生時に CinematicOverlay から Sprite の有無に応じて解決する
        private readonly Image _customImage;
        private Image _activeImage;

        /// <summary>
        /// オーバーレイ Image を自動取得するコンストラクタ。 <see cref="CinematicOverlay"/> が
        /// 必要な Canvas + Image をシーン上に自動生成するため、 事前配置は不要。
        /// </summary>
        public ImageFlashEffect() { }

        public ImageFlashEffect(Image overlayImage) : base()
        {
            _customImage = overlayImage;
            OnResetImmediate();
        }

        // Sprite 指定時はポストプロセスの掛かるカメラ空間オーバーレイに描き、ソリッドカラーは最前面オーバーレイに描く
        private Image ResolveImage() =>
            _customImage != null ? _customImage
            : CurrentConfig.Sprite != null ? CinematicOverlay.Instance.CameraSpaceImage
            : CinematicOverlay.Instance.Image;

        protected override async UniTask OnPlayAsync(CancellationToken ct)
        {
            var image = _activeImage = ResolveImage();
            image.raycastTarget = false;
            image.transform.SetAsLastSibling();

            // Sprite 指定時は画像フラッシュ、未指定ならソリッドカラー
            image.sprite = CurrentConfig.Sprite;
            image.preserveAspect = CurrentConfig.PreserveAspect;

            // ティントカラーを適用してアルファを 0 から開始
            var color = CurrentConfig.TintColor;
            color.a = 0f;
            image.color = color;

            // フェードイン → ホールド → フェードアウト
            await image.FadeIn(CurrentConfig.EnterDuration, CurrentConfig.Ease).ToUniTask(cancellationToken: ct);

            if (CurrentConfig.HoldDuration > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(CurrentConfig.HoldDuration), cancellationToken: ct);
            }

            await image.FadeOut(CurrentConfig.ExitDuration, CurrentConfig.Ease).ToUniTask(cancellationToken: ct);
            ResetImage(image);
        }

        protected override async UniTask OnStopAsync(CancellationToken ct)
        {
            var image = _activeImage != null ? _activeImage : ResolveImage();
            await image.FadeOut(CurrentConfig.ExitDuration, CurrentConfig.Ease).ToUniTask(cancellationToken: ct);
            ResetImage(image);
        }

        protected override void OnResetImmediate()
        {
            var image = _activeImage != null ? _activeImage : _customImage;
            if (image == null) return;

            image.raycastTarget = false;
            ResetImage(image);
        }

        private static void ResetImage(Image image)
        {
            var color = image.color;
            color.a = 0f;
            image.color = color;
            // オーバーレイ Image は ScreenFadeEffect と共有のため、スプライトを残さない
            image.sprite = null;
        }
    }
}
