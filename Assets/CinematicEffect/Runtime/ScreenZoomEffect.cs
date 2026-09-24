using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

namespace Void2610.CinematicEffect
{
    /// <summary>
    /// 描画済みの画面を注視点へ向けて拡大する (カメラが寄って見える)。マテリアル (ScreenZoom.shader) は未配線時 Resources から自己調達する。
    /// Screen Space - Camera の Canvas はカメラの視野に追従して縮むため、UI にはカメラ側のズームが効かない。画面ごと拡大すれば UI も一緒に寄る。
    /// AutoComplete=true なら Play 単体で寄る→保持→戻るまで自己完結する。false なら Play は寄ったまま StopAsync を待つ
    /// </summary>
    public sealed class ScreenZoomEffect : ConfigurableCinematicEffectBase<ScreenZoomConfig>
    {
        public override string EffectName => "画面ズーム";

        private static readonly int ZoomId = Shader.PropertyToID("_Zoom");
        private static readonly int CenterId = Shader.PropertyToID("_Center");

        private const string DefaultMaterialResourcePath = "ScreenZoom";

        private readonly Material _material;
        private float _currentZoom = 1f;

        // 連続で寄り直すとき、等倍へ飛ばずに今の拡大率から寄る
        protected override bool ResetVisualsOnReplay => false;

        // 未配線時は同梱マテリアルを自己ロードする (RadialBlur と同じ自己調達フォールバック。事前配置不要)
        public ScreenZoomEffect() : this(LoadDefaultMaterial()) { }

        public ScreenZoomEffect(Material screenZoomMaterial) : base()
        {
            _material = screenZoomMaterial;
            OnResetImmediate();
        }

        // RendererFeature と同一マテリアルアセットを共有する。パス設定ミスを後段の NRE ではなくロード時点で顕在化させる
        private static Material LoadDefaultMaterial()
        {
            var material = Resources.Load<Material>(DefaultMaterialResourcePath);
            if (material == null) throw new InvalidOperationException($"Resources から ScreenZoom マテリアルをロードできません: {DefaultMaterialResourcePath}");

            return material;
        }

        protected override async UniTask OnPlayAsync(CancellationToken ct)
        {
            // RendererFeature はレンダラ資産へ事前配置せず、初回再生時にコードで注入する (可搬性優先)
            CinematicRendererFeatureInjector.EnsureFeature<ScreenZoomRendererFeature>();

            _material.SetVector(CenterId, CurrentConfig.Center);
            ScreenZoomRendererFeature.Active = true;

            await AnimateZoomAsync(_currentZoom, CurrentConfig.Zoom, CurrentConfig.EnterDuration, Ease.OutCubic, ct);

            if (!CurrentConfig.AutoComplete)
            {
                await UniTask.WaitUntilCanceled(ct);
                return;
            }

            if (CurrentConfig.HoldDuration > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(CurrentConfig.HoldDuration), Cysharp.Threading.Tasks.DelayType.DeltaTime, PlayerLoopTiming.Update, ct);
            await AnimateZoomAsync(_currentZoom, 1f, CurrentConfig.ExitDuration, CurrentConfig.Ease, ct);

            OnResetImmediate();
        }

        protected override async UniTask OnStopAsync(CancellationToken ct)
        {
            await AnimateZoomAsync(_currentZoom, 1f, CurrentConfig.ExitDuration, CurrentConfig.Ease, ct);
            OnResetImmediate();
        }

        protected override void OnResetImmediate()
        {
            if (_material == null) return;
            _currentZoom = 1f;
            _material.SetFloat(ZoomId, 1f);
            ScreenZoomRendererFeature.Active = false;
        }

        private async UniTask AnimateZoomAsync(float from, float to, float duration, Ease ease, CancellationToken ct)
        {
            if (duration <= 0f)
            {
                SetZoom(to);
                return;
            }

            await LMotion.Create(from, to, duration).WithEase(ease).Bind(SetZoom).ToUniTask(cancellationToken: ct);
        }

        private void SetZoom(float zoom)
        {
            _currentZoom = zoom;
            _material.SetFloat(ZoomId, zoom);
        }
    }
}
