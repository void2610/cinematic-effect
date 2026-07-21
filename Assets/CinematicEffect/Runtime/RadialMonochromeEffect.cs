using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

namespace Void2610.CinematicEffect
{
    /// <summary>
    /// 中心からカラーが「ブワッ」と全画面へ広がる演出。
    /// AutoComplete=true なら Play 単体で広がる→保持→収縮まで自己完結する (PlayAndAwait 前提、旧挙動)。
    /// AutoComplete=false なら Play は広がった状態で StopAsync まで保持し、Stop 側で「ゆっくり収縮して白黒へ戻る」を再生する
    /// (シナリオ上で「目が開く→数秒間カラー→テキスト表示前に収縮」のように Enter/Exit を別タイミングで発火したいケース向け)。
    /// パラメータ (時間・ぼかし) は <see cref="RadialMonochromeConfig"/> = SO 側で調整する。RendererFeature と同一マテリアルを Resources 共有する (VisionWarp と同じ自己調達)。
    /// </summary>
    public sealed class RadialMonochromeEffect : ConfigurableCinematicEffectBase<RadialMonochromeConfig>
    {
        public override string EffectName => "モノクロ収縮";

        private static readonly int RadiusId = Shader.PropertyToID("_Radius");
        private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
        private static readonly int CenterId = Shader.PropertyToID("_Center");
        private static readonly int AspectId = Shader.PropertyToID("_Aspect");
        private static readonly int MaskTexId = Shader.PropertyToID("_MaskTex");
        private static readonly int MaskStrengthId = Shader.PropertyToID("_MaskStrength");

        private const string DefaultMaterialResourcePath = "RadialMonochrome";

        private readonly Material _material;

        public RadialMonochromeEffect() : this(LoadDefaultMaterial()) { }

        public RadialMonochromeEffect(Material material) : base()
        {
            _material = material;
            OnResetImmediate();
        }

        // RendererFeature と同一マテリアルアセットを共有する。パス設定ミスを後段の NRE ではなくロード時点で顕在化させる
        private static Material LoadDefaultMaterial()
        {
            var material = Resources.Load<Material>(DefaultMaterialResourcePath);
            if (material == null) throw new InvalidOperationException($"Resources から RadialMonochrome マテリアルをロードできません: {DefaultMaterialResourcePath}");

            return material;
        }

        // AutoComplete=false なら広げて保持したまま StopAsync を待つ (Play/Stop を別タイミングで発火する用途)。true なら旧挙動 (自己完結)
        protected override async UniTask OnPlayAsync(CancellationToken ct)
        {
            // RendererFeature はレンダラ資産へ事前配置せず、初回再生時にコードで注入する (可搬性優先)
            CinematicRendererFeatureInjector.EnsureFeature<RadialMonochromeRendererFeature>();

            _material.SetVector(CenterId, new Vector4(0.5f, 0.5f, 0f, 0f));
            _material.SetFloat(SoftnessId, CurrentConfig.Softness);
            var aspect = (float)Screen.width / Screen.height;
            _material.SetFloat(AspectId, aspect);
            // MaskTexture 未設定時は _MaskStrength=0 が効くため境界は真円のまま (旧挙動と同一)
            if (CurrentConfig.MaskTexture != null) _material.SetTexture(MaskTexId, CurrentConfig.MaskTexture);
            _material.SetFloat(MaskStrengthId, CurrentConfig.MaskTexture != null ? CurrentConfig.MaskStrength : 0f);
            // 中心から画面隅までの最大距離 (アスペクト補正済み)。ここまで広げれば全画面カラーになる
            var full = Mathf.Sqrt(0.25f * aspect * aspect + 0.25f) + 0.15f;
            RadialMonochromeRendererFeature.Active = true;

            await AnimateRadiusAsync(0f, full, CurrentConfig.EnterDuration, Ease.OutCubic, ct); // ブワッと色が広がる

            if (!CurrentConfig.AutoComplete)
            {
                // Stop が呼ばれるまでカラーが全画面に広がったまま保持する
                await UniTask.WaitUntilCanceled(ct);
                return;
            }

            if (CurrentConfig.HoldDuration > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(CurrentConfig.HoldDuration), Cysharp.Threading.Tasks.DelayType.DeltaTime, PlayerLoopTiming.Update, ct);
            await AnimateRadiusAsync(full, 0f, CurrentConfig.ExitDuration, CurrentConfig.Ease, ct); // ゆっくり白黒が収縮

            OnResetImmediate();
        }

        protected override async UniTask OnStopAsync(CancellationToken ct)
        {
            // Play 側の広がり切った半径から、ゆっくり白黒へ収縮させる
            var from = _material.GetFloat(RadiusId);
            await AnimateRadiusAsync(from, 0f, CurrentConfig.ExitDuration, CurrentConfig.Ease, ct);
            _material.SetFloat(MaskStrengthId, 0f);
            // radius 0 は全画面白黒。収縮直後に Active を落とすと素のカラー映像が露出するため、止めるのは収縮量ゼロで再 Stop されたとき (色を戻す側) だけ
            if (from <= 0.0001f) RadialMonochromeRendererFeature.Active = false;
        }

        protected override void OnResetImmediate()
        {
            if (_material == null) return;
            _material.SetFloat(RadiusId, 0f); // 半径 0 = 全画面白黒 (収縮しきった状態)
            _material.SetFloat(MaskStrengthId, 0f);
            RadialMonochromeRendererFeature.Active = false;
        }

        private async UniTask AnimateRadiusAsync(float from, float to, float duration, Ease ease, CancellationToken ct)
        {
            if (duration <= 0f)
            {
                _material.SetFloat(RadiusId, to);
                return;
            }

            await LMotion.Create(from, to, duration)
                .WithEase(ease)
                .Bind(this, (value, self) => self._material.SetFloat(RadiusId, value))
                .ToUniTask(cancellationToken: ct);
        }
    }
}
