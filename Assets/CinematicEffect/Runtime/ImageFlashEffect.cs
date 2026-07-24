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
        private readonly Image _overlayImage;

        /// <summary>
        /// オーバーレイ Image を自動取得するコンストラクタ。 <see cref="CinematicOverlay"/> が
        /// 必要な Canvas + Image をシーン上に自動生成するため、 事前配置は不要。
        /// </summary>
        public ImageFlashEffect() : this(CinematicOverlay.Instance.Image) { }

        public ImageFlashEffect(Image overlayImage) : base()
        {
            _overlayImage = overlayImage;
            OnResetImmediate();
        }

        protected override async UniTask OnPlayAsync(CancellationToken ct)
        {
            _overlayImage.raycastTarget = false;
            _overlayImage.transform.SetAsLastSibling();

            // Sprite 指定時は画像フラッシュ、未指定ならソリッドカラー
            _overlayImage.sprite = CurrentConfig.Sprite;
            _overlayImage.preserveAspect = CurrentConfig.PreserveAspect;

            // ティントカラーを適用してアルファを 0 から開始
            var color = CurrentConfig.TintColor;
            color.a = 0f;
            _overlayImage.color = color;

            // フェードイン → ホールド → フェードアウト
            await _overlayImage.FadeIn(CurrentConfig.EnterDuration, CurrentConfig.Ease).ToUniTask(cancellationToken: ct);

            if (CurrentConfig.HoldDuration > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(CurrentConfig.HoldDuration), cancellationToken: ct);
            }

            await _overlayImage.FadeOut(CurrentConfig.ExitDuration, CurrentConfig.Ease).ToUniTask(cancellationToken: ct);
            ResetAlpha();
        }

        protected override async UniTask OnStopAsync(CancellationToken ct)
        {
            await _overlayImage.FadeOut(CurrentConfig.ExitDuration, CurrentConfig.Ease).ToUniTask(cancellationToken: ct);
            ResetAlpha();
        }

        protected override void OnResetImmediate()
        {
            if (_overlayImage == null) return;

            _overlayImage.raycastTarget = false;
            ResetAlpha();
        }

        private void ResetAlpha()
        {
            var color = _overlayImage.color;
            color.a = 0f;
            _overlayImage.color = color;
            // オーバーレイ Image は ScreenFadeEffect と共有のため、スプライトを残さない
            _overlayImage.sprite = null;
        }
    }
}
