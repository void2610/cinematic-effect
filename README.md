# cinematic-effect

コードから宣言的に演出を再生する Unity 向けシネマティックエフェクト集。

画面フェード / フラッシュ / 点滅 / レターボックス / カメラシェイク (Perlin / 単発 / 方向感覚喪失) / ポストプロセス演出 / URP RendererFeature ベースの放射ブラー・放射モノクロ・視界歪み等を、シーンへの事前配置なしに再生できる。

> 初めて使う場合は [チュートリアル](docs/tutorial.md) から読むことを推奨。

## 特徴

- **事前配置不要**: オーバーレイ Canvas / PostProcess Volume / RendererFeature は初回再生時に自動生成・注入される
- **UniTask ベース**: すべての演出は `UniTask` を返し、`await` / キャンセルに対応
- **宣言的シーケンス**: `CinematicSequence` のメソッドチェーンで「開始 → 待機 → 停止」を記述し、実行は `CinematicEffectDirector` に委ねる
- **アセット化**: `CinematicSequenceAsset` で演出列を ScriptableObject として保存し、Inspector から編集・再生できる
- **Editor プレビュー**: `CinematicTestWindow` (Window > Cinematic Test) から各演出を Editor 上で試せる
- **拡張可能**: `ConfigurableCinematicEffectBase<TConfig>` を継承して独自演出を追加できる

## インストール

Package Manager の *Add package from git URL...* に以下を指定:

```text
https://github.com/void2610/cinematic-effect.git?path=Assets/CinematicEffect
```

バージョンを固定したい場合は URL 末尾に `#<コミットSHA>` を付けてピン留めする (更新は SHA の付け替え)。

### 依存パッケージ

以下は git URL 依存のため自動解決されない。利用側プロジェクトの `Packages/manifest.json` に追加すること:

- [UniTask](https://github.com/Cysharp/UniTask) (`com.cysharp.unitask`)
- [LitMotion](https://github.com/AnnulusGames/LitMotion) (`com.annulusgames.lit-motion`)

また URP (`com.unity.render-pipelines.universal`) が必須。

## クイックスタート

シーンの任意の GameObject に `CinematicEffectDirector` を追加し (VContainer なら `RegisterComponent` で注入)、シーケンスを組んで `RunAsync` に渡す:

```csharp
using Void2610.CinematicEffect;

// 「フェードで暗転 → 0.5 秒待機 → フェード解除」
var sequence = CinematicSequence.Create()
    .PlayAndAwait<ScreenFadeEffect>()
    .Delay(0.5f)
    .Stop<ScreenFadeEffect>();
await director.RunAsync(sequence, ct);

// アセット化した演出列の再生
[SerializeField] private CinematicSequenceAsset sequenceAsset;
await director.RunAsync(sequenceAsset, ct);
```

各エフェクトは対応する Config でパラメータを上書きできる (省略時はデフォルト値):

```csharp
var seq = CinematicSequence.Create()
    .Play<VignetteEffect>(new VignetteConfig(volumeWeight: 1f, intensity: 0.45f, smoothness: 0.8f, enterDuration: 0.6f, exitDuration: 0.8f))
    .PlayAndAwait<CameraShakeEffect>(new CameraShakeConfig(magnitude: 0.15f, duration: 0.4f, loop: false))
    .Stop<VignetteEffect>();
```

### ステップの種類

| メソッド | 意味 | 主な用途 |
| --- | --- | --- |
| `Play<T>(config)` | Fire-and-Forget で開始 | ループ型 (ビネット・ノイズ等) を掛けっぱなしにする |
| `PlayAndAwait<T>(config)` | 完了を待って次へ | 1 回完結型 (フェード・フラッシュ・単発シェイク) |
| `Stop<T>()` | 停止アニメーション完了を待つ | ループ型の解除 |
| `Delay(seconds)` | 指定秒待機 | 演出間の間 (ま) |

## エフェクト一覧

### オーバーレイ / カメラ系 (シーン配線不要)

| エフェクト | Config | 内容 |
| --- | --- | --- |
| `ScreenFadeEffect` | `ScreenFadeConfig` | 画面フェード (色・hold・autoComplete 指定可) |
| `ImageFlashEffect` | `ImageFlashConfig` | フラッシュ / 暗転 (tint 色指定) |
| `BlinkEffect` | `BlinkConfig` | 断続的な明滅ループ |
| `LetterboxEffect` | `LetterboxConfig` | 上下黒帯レターボックス |
| `CameraShakeEffect` | `CameraShakeConfig` | ランダム振動シェイク (単発 / ループ) |
| `CameraPerlinShakeEffect` | `CameraPerlinShakeConfig` | パーリンノイズによる滑らかな揺れ |
| `CameraDisorientationEffect` | `CameraDisorientationConfig` | 傾き + ズーム + 揺らぎの方向感覚喪失 |

### URP RendererFeature 系 (自動注入)

| エフェクト | Config | 内容 |
| --- | --- | --- |
| `RadialBlurEffect` * | `RadialBlurConfig` | 放射状 (ズーム) ブラー |
| `RadialMonochromeEffect` | `RadialMonochromeConfig` | 円形にカラー ⇄ モノクロを遷移 |
| `VisionWarpEffect` | `VisionWarpConfig` | 陽炎・酩酊感のある視界歪み |
| `WaveDistortionEffect` | `WaveDistortionConfig` | 波打ち画面歪み |

\* `RadialBlurEffect` は現状 `CinematicEffectDirector` に未登録のため、シーケンスから直接は再生できない (直接インスタンス化して利用する)。

### PostProcess Volume 系 (Director にぶら下がる Volume を自動生成)

| エフェクト | Config | 内容 |
| --- | --- | --- |
| `VignetteEffect` | `VignetteConfig` | ビネット |
| `PulseVignetteEffect` | `PulseVignetteConfig` | 脈動ビネット (動悸感) |
| `ChromaticAberrationEffect` | `ChromaticAberrationConfig` | 色収差 |
| `LensDistortionEffect` | `LensDistortionConfig` | レンズ歪み |
| `FilmGrainEffect` | `FilmGrainConfig` | フィルムグレイン |
| `FilmNoiseEffect` | `FilmNoiseConfig` | 古いフィルム風ノイズ (スクラッチ / コマ揺れ) |
| `ContrastEffect` | `ContrastConfig` | コントラスト |
| `SaturationEffect` | `SaturationConfig` | 彩度 |
| `ColorFilterEffect` | `ColorFilterConfig` | 乗算カラーフィルター |
| `ColorGradeEffect` | `ColorGradeConfig` | 彩度 + フィルター + コントラスト + 露出の一括制御 |
| `DepthOfFieldEffect` | `DepthOfFieldConfig` | 被写界深度ぼかし |
| `DoFDizzinessEffect` | `DoFDizzinessConfig` | ぼかし ON/OFF ループのめまい表現 |

## Director のユーティリティ

```csharp
CinematicEffectDirector.IsEffectsEnabled = false; // 全演出を無効化 (RunAsync が即 return)
director.IsPlaying(typeof(VignetteEffect));       // 再生中かの観測
director.ResetAll();                              // 全演出をアニメーションなしで即リセット
```

## Editor プレビュー (CinematicTestWindow)

**Window > Cinematic Test** からテスト用ウィンドウを開き、ステップを組んでプレイモードで再生できる。再生中のステップはハイライト表示される。`CinematicEffectDirector` の Inspector からもこのウィンドウを開ける。

## ドキュメント

- [チュートリアル](docs/tutorial.md) — 導入から独自エフェクト作成まで
- [CHANGELOG](Assets/CinematicEffect/CHANGELOG.md)

## License

MIT
