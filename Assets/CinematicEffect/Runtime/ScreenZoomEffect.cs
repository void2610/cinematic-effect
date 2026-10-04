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
        private static readonly int ViewCenterId = Shader.PropertyToID("_ViewCenter");
        private static readonly int RotationId = Shader.PropertyToID("_Rotation");
        private static readonly int AspectId = Shader.PropertyToID("_Aspect");

        private const string DefaultMaterialResourcePath = "ScreenZoom";

        private readonly Material _material;
        private float _currentZoom = 1f;

        /// <summary>いま画面を描いている拡大率 (回転による底上げ込み)。寄りに合わせて画面上の大きさを保ちたい要素の打ち消しに使う</summary>
        public float CurrentMagnification { get; private set; } = 1f;

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

            ScreenZoomRendererFeature.Active = true;

            await AnimateZoomAsync(_currentZoom, CurrentConfig.Zoom, CurrentConfig.EnterDuration, CurrentConfig.EnterEase, ct);

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
            CurrentMagnification = 1f;
            _material.SetFloat(ZoomId, 1f);
            _material.SetVector(ViewCenterId, new Vector2(0.5f, 0.5f));
            _material.SetFloat(RotationId, 0f);
            _material.SetFloat(AspectId, (float)Screen.width / Screen.height);
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
            var progress = Progress(zoom);
            var angle = CurrentConfig.Rotation * progress * Mathf.Deg2Rad;
            var aspect = (float)Screen.width / Screen.height;
            // 回した分だけ画面の角が外へはみ出すので、外をサンプルしない拡大率まで底上げする
            var sampleZoom = Mathf.Max(zoom, CoverZoom(angle, aspect));
            CurrentMagnification = sampleZoom;
            _material.SetFloat(ZoomId, sampleZoom);
            _material.SetFloat(RotationId, angle);
            _material.SetFloat(AspectId, aspect);
            _material.SetVector(ViewCenterId, ViewCenterFor(sampleZoom, progress, angle, aspect));
        }

        // 寄り切りを 1 とした拡大の進み具合。中央へ運ぶ寄りと回転はこれに合わせて進む
        private float Progress(float zoom)
        {
            var peak = CurrentConfig.Zoom;
            return peak > 1f ? Mathf.Clamp01((zoom - 1f) / (peak - 1f)) : 1f;
        }

        // 画面と同じ縦横比の窓を angle 回しても元画面に収まる最小の拡大率
        private static float CoverZoom(float angle, float aspect)
        {
            var cos = Mathf.Abs(Mathf.Cos(angle));
            var sin = Mathf.Abs(Mathf.Sin(angle));
            return Mathf.Max(cos + sin / aspect, cos + sin * aspect);
        }

        // 画面中央に映す位置。注視点を固定する寄りは C + (0.5 - C) / zoom、中央へ運ぶ寄りは拡大の進み具合で 0.5 から C へ移す
        private Vector2 ViewCenterFor(float zoom, float progress, float angle, float aspect)
        {
            var center = CurrentConfig.Center;
            var z = Mathf.Max(zoom, 1f);
            if (!CurrentConfig.BringToCenter)
            {
                var fixedCenter = center + (new Vector2(0.5f, 0.5f) - center) / z;
                // 回さなければ注視点を固定する寄りは元画面の内側しか映さない
                if (Mathf.Approximately(angle, 0f)) return fixedCenter;
                return ClampInside(fixedCenter, z, angle, aspect);
            }

            return ClampInside(Vector2.Lerp(new Vector2(0.5f, 0.5f), center, progress), z, angle, aspect);
        }

        private static Vector2 ClampInside(Vector2 viewCenter, float z, float angle, float aspect)
        {
            // 見えている範囲 (回転込みの外接矩形) の半分より端へ寄せると画面外をサンプルして端の色が伸びるため収める
            var cos = Mathf.Abs(Mathf.Cos(angle));
            var sin = Mathf.Abs(Mathf.Sin(angle));
            var halfX = (cos + sin / aspect) * 0.5f / z;
            var halfY = (cos + sin * aspect) * 0.5f / z;
            return new Vector2(Mathf.Clamp(viewCenter.x, halfX, 1f - halfX), Mathf.Clamp(viewCenter.y, halfY, 1f - halfY));
        }
    }
}
