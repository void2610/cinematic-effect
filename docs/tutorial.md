# チュートリアル

導入から独自エフェクトの追加まで、cinematic-effect を段階的に使えるようになるためのガイド。

## 目次

1. [セットアップ](#1-セットアップ)
2. [最初の演出を再生する](#2-最初の演出を再生する)
3. [シーケンスを組む](#3-シーケンスを組む)
4. [Config でパラメータを調整する](#4-config-でパラメータを調整する)
5. [演出をアセット化する](#5-演出をアセット化する)
6. [Editor でプレビューする](#6-editor-でプレビューする)
7. [キャンセルと後始末](#7-キャンセルと後始末)
8. [独自エフェクトを追加する](#8-独自エフェクトを追加する)

## 1. セットアップ

### パッケージの追加

Package Manager の *Add package from git URL...* に以下を指定する:

```text
https://github.com/void2610/cinematic-effect.git?path=Assets/CinematicEffect
```

UniTask / LitMotion は git URL 依存のため自動解決されない。`Packages/manifest.json` に追加する:

```json
{
  "dependencies": {
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
    "com.annulusgames.lit-motion": "https://github.com/AnnulusGames/LitMotion.git?path=src/LitMotion/Assets/LitMotion"
  }
}
```

レンダーパイプラインは URP 前提。歪み系エフェクトのマテリアルはパッケージ同梱の Resources から自動ロードされるため、追加のアセット設定は不要。

### Director の配置

演出の実行はすべて `CinematicEffectDirector` (MonoBehaviour) が担う。シーンの任意の GameObject に AddComponent しておく。

VContainer を使う場合は LifetimeScope で登録すると Presenter 等へ注入できる:

```csharp
protected override void Configure(IContainerBuilder builder)
{
    builder.RegisterComponentInHierarchy<CinematicEffectDirector>();
}
```

DI を使わないなら `FindFirstObjectByType<CinematicEffectDirector>()` や SerializeField 参照でよい。

オーバーレイ Canvas・PostProcess Volume・RendererFeature は**初回再生時に自動生成・注入される**ため、シーン側の事前配線は Director の配置だけで完了する。

## 2. 最初の演出を再生する

「黒フェードで暗転し、0.5 秒置いて明ける」だけの最小例:

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using Void2610.CinematicEffect;

public async UniTask PlayFadeAsync(CinematicEffectDirector director, CancellationToken ct)
{
    var sequence = CinematicSequence.Create()
        .PlayAndAwait<ScreenFadeEffect>() // 暗転が完了するまで待つ
        .Delay(0.5f)                      // 暗転を保持
        .Stop<ScreenFadeEffect>();        // 明けるアニメーションの完了まで待つ

    await director.RunAsync(sequence, ct);
}
```

ポイント:

- `CinematicSequence` は**純粋なデータ**。組んだ時点では何も起きず、`RunAsync` に渡して初めて実行される
- エフェクトは型で指定する。インスタンスの生成・管理は Director が内部で行う

## 3. シーケンスを組む

ステップは 4 種類。使い分けが演出設計の中心になる。

| メソッド | 挙動 | 使いどころ |
| --- | --- | --- |
| `Play<T>()` | 開始して**待たずに**次のステップへ | ループ型 (ビネット・ノイズ・シェイク持続) を掛けっぱなしにする |
| `PlayAndAwait<T>()` | 開始して**完了まで待つ** | 1 回完結型 (フェード・フラッシュ・単発シェイク) |
| `Stop<T>()` | 停止アニメーションの完了まで待つ | `Play` で開始したループ型の解除 |
| `Delay(sec)` | 指定秒待機 | 演出間の間 (ま) |

複数エフェクトの重ね掛けは `Play` を連ねる:

```csharp
// 回想シーン導入: レターボックス + セピア調 + フィルムノイズを重ね、フラッシュで転換
var intro = CinematicSequence.Create()
    .Play<LetterboxEffect>()
    .Play<ColorGradeEffect>()
    .Play<FilmNoiseEffect>()
    .PlayAndAwait<ImageFlashEffect>();

// 回想終了: 掛けたものを順に解除
var outro = CinematicSequence.Create()
    .Stop<FilmNoiseEffect>()
    .Stop<ColorGradeEffect>()
    .Stop<LetterboxEffect>();
```

`Stop` は解除アニメーションを待つため、複数同時に解除したい場合はエフェクト側の `ExitDuration` を揃えるか、解除順を演出として設計する。

型を実行時に決めたい場合は非ジェネリック版 `Play(Type, config)` も使える。

## 4. Config でパラメータを調整する

各エフェクトには対応する Config クラスがあり、`Play` / `PlayAndAwait` の引数で渡す。**省略するとデフォルト値**で再生される。

```csharp
var seq = CinematicSequence.Create()
    .PlayAndAwait<ScreenFadeEffect>(new ScreenFadeConfig(
        fadeColor: Color.white,
        fadeInDuration: 0.2f,
        fadeOutDuration: 1.0f,
        holdDuration: 0.3f,
        ease: Ease.OutQuad,
        autoComplete: true)) // hold 後に自動で明ける (Stop 不要)
    .Play<CameraPerlinShakeEffect>(new CameraPerlinShakeConfig(magnitude: 0.05f, speed: 0.8f));
```

Config の階層は共通パラメータを持つ:

- `TimedEffectConfig` — `EnterDuration` (開始遷移) / `ExitDuration` (終了遷移) / `Ease`
- `HoldableEffectConfig` — 上に加えて `HoldDuration` / `AutoComplete` (hold 後に自動で解除するか)
- `VolumeEffectConfig` — PostProcess 系共通の `VolumeWeight`

Config を受け取れないステップに Config を渡すと実行時に `InvalidOperationException` になるため、エフェクトと Config の対応 (README のエフェクト一覧参照) を守ること。

Config は Director 側で `Clone()` されるため、同じ Config インスタンスを複数ステップで使い回しても安全。

## 5. 演出をアセット化する

コードで組む代わりに、演出列を ScriptableObject として保存し Inspector で編集できる。

1. Project ビューで **Create > Cinematic > Sequence Asset** を選ぶ
2. Inspector で Step を追加し、種別 (Play / PlayAndAwait / Stop / Delay) とエフェクトを選ぶ
3. **Use Custom Config** を有効にすると、そのエフェクト専用のパラメータ欄が表示される (無効ならデフォルト値)

再生はコード版と同じ `RunAsync`:

```csharp
[SerializeField] private CinematicSequenceAsset flashbackSequence;

await director.RunAsync(flashbackSequence, ct);
```

内部的には `Build()` で `CinematicSequence` に変換されるだけなので、コード組みとアセットは自由に併用できる。「デザイナーが調整する演出はアセット、ゲーム状態に依存して動的に変わる演出はコード」という分担が目安。

## 6. Editor でプレビューする

**Window > Cinematic Test** でテストウィンドウを開くと、ステップ列を組んでプレイモードで再生できる。再生中のステップはハイライトされるため、タイミング調整に使える。

`CinematicEffectDirector` の Inspector にもこのウィンドウを開くボタンがある。

## 7. キャンセルと後始末

- すべての演出は `CancellationToken` で中断できる。シーン遷移やスキップ操作の ct を `RunAsync` に渡すこと
- `director.ResetAll()` で全演出をアニメーションなしで即座に初期状態へ戻せる。スキップ処理や状態の作り直しに使う
- `director.IsPlaying(typeof(VignetteEffect))` で個別エフェクトの再生状態を観測できる
- `CinematicEffectDirector.IsEffectsEnabled = false` にすると `RunAsync` が即 return する。演出オフ設定やテストの高速化に使う

```csharp
// スキップ可能なイベント演出の典型形
try
{
    await director.RunAsync(sequence, skipCts.Token);
}
catch (OperationCanceledException)
{
    director.ResetAll(); // スキップ時は演出を即時解消
}
```

## 8. 独自エフェクトを追加する

`ConfigurableCinematicEffectBase<TConfig>` を継承すると、Config の適用・Clone・リセットの仕組みに乗った演出を追加できる。

### Config を定義する

```csharp
[Serializable]
public sealed class MyGlitchConfig : TimedEffectConfig
{
    [SerializeField] private float intensity = 0.5f;

    public float Intensity => intensity;

    public MyGlitchConfig() { }

    public MyGlitchConfig(float intensity, float enterDuration, float exitDuration, Ease ease = Ease.InOutSine)
        : base(enterDuration, exitDuration, ease)
    {
        this.intensity = intensity;
    }

    public override CinematicEffectConfig Clone() => new MyGlitchConfig(intensity, EnterDuration, ExitDuration, Ease);
}
```

### エフェクト本体を実装する

```csharp
public sealed class MyGlitchEffect : ConfigurableCinematicEffectBase<MyGlitchConfig>
{
    public override string EffectName => "グリッチ";

    // Enter 遷移 → (ループ型なら ct キャンセルまでホールド)
    protected override async UniTask OnPlayAsync(CancellationToken ct)
    {
        // CurrentConfig に、ステップで渡された Config (未指定ならデフォルト) が入っている
        await AnimateBlendAsync(0f, 1f, CurrentConfig.EnterDuration, CurrentConfig.Ease, ApplyBlend, ct);
    }

    // Exit 遷移 (逆アニメーション)
    protected override async UniTask OnStopAsync(CancellationToken ct)
    {
        await AnimateBlendAsync(1f, 0f, CurrentConfig.ExitDuration, CurrentConfig.Ease, ApplyBlend, ct);
    }

    // アニメーションなしの即時リセット
    protected override void OnResetImmediate() => ApplyBlend(0f);

    private void ApplyBlend(float t)
    {
        // t (0..1) と CurrentConfig.Intensity を使って実際の描画状態を更新する
    }
}
```

実装の分担:

- 基底クラス (`CinematicEffectBase`) が `IsPlaying` 管理・再入時のリセットを担う
- 派生クラスは `OnPlayAsync` / `OnStopAsync` / `OnResetImmediate` の 3 点だけを書く
- 補間には基底の `AnimateBlendAsync` ヘルパー (LitMotion ベース) が使える

### Director へ登録する

エフェクトの登録は現状 `CinematicEffectDirector.EnsureEffectsRegistered` 内にハードコードされている。パッケージ外から独自エフェクトを足す場合はここへの追記が必要 (= パッケージ側の改修)。利用プロジェクト側での回避策を組むより、本体へ登録 API を追加する方針を推奨。

### 参考にすべき既存実装

- 1 回完結型: `ImageFlashEffect` / `ScreenFadeEffect`
- ループ型: `BlinkEffect` / `PulseVignetteEffect`
- PostProcess Volume 系: `PostProcessVolumeEffectBase` 派生 (`PostProcessEffects.cs`)
- RendererFeature 系: `RadialBlurEffect` + `RadialBlurRendererFeature`
