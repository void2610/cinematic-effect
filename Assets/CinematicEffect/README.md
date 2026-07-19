# cinematic-effect

コードから宣言的に演出を再生する Unity 向けシネマティックエフェクト集。

画面フェード / フラッシュ / 点滅 / レターボックス / カメラシェイク (Perlin / 単発 / 方向感覚喪失) / ポストプロセス演出 / URP RendererFeature ベースの放射ブラー・放射モノクロ・視界歪み等を、シーンへの事前配置なしに再生できる。

## 特徴

- **事前配置不要**: オーバーレイ Canvas / PostProcess Volume / RendererFeature は初回再生時に自動生成・注入される
- **UniTask ベース**: すべての演出は `UniTask` を返し、`await` / キャンセルに対応
- **アセット化**: `CinematicSequenceAsset` で演出列を ScriptableObject として保存し、`CinematicEffectDirector` で再生できる
- **Editor プレビュー**: `CinematicTestWindow` から各演出を Editor 上で試せる

## インストール

Package Manager の *Add package from git URL...* に以下を指定:

```text
https://github.com/void2610/cinematic-effect.git?path=Assets/CinematicEffect
```

### 依存パッケージ

以下は git URL 依存のため自動解決されない。利用側プロジェクトの `Packages/manifest.json` に追加すること:

- [UniTask](https://github.com/Cysharp/UniTask) (`com.cysharp.unitask`)
- [LitMotion](https://github.com/AnnulusGames/LitMotion) (`com.annulusgames.lit-motion`)

また URP (`com.unity.render-pipelines.universal`) が必須。

## 使い方

```csharp
using Void2610.CinematicEffect;

// コードで組んだ演出列の再生
var sequence = CinematicSequence.Create()
    .Play(typeof(ScreenFadeEffect))
    .Delay(0.5f)
    .Stop(typeof(ScreenFadeEffect));
await director.RunAsync(sequence, ct);

// アセット化した演出列の再生
[SerializeField] private CinematicSequenceAsset sequenceAsset;
await director.RunAsync(sequenceAsset, ct);
```

## License

MIT
