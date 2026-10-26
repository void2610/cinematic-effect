# Changelog

## [Unreleased]

### Added

- `ICinematicEffect.IsPlayingChanged` / `CinematicEffectDirector.EffectPlayingChanged` を追加。
  再生状態をポーリングせずに追えるようにするためのイベント

### Fixed

- `CinematicSequence.Play` で起動した演出が `RunAsync` の ct キャンセルで打ち切られ、
  見た目 (暗転・彩度等) を残したまま `IsPlaying` が false になる不整合を修正。
  `Play` の演出は Director の破棄まで生き、`Stop` / `ResetAll` / 再 Play で終わる
- `CinematicRendererFeatureInjector` が注入したフィーチャを Play 終了後もレンダラ資産のリストに残し、
  その後に資産を保存すると `{fileID: 0}` として焼き込まれ、URP の「missing RendererFeatures」エラーになる不具合を修正。
  エディタの Play 終了時に注入分を外して破棄し、ドメインリロード無効でも次の Play で注入し直す

## [0.1.0] - 2026-07-19

### Added

- my-unity-utils の `CinematicEffect/` を独立 UPM パッケージとして切り出し
- 全クラスを `Void2610.CinematicEffect` 名前空間へ移動
- my-unity-utils (`Void2610.UnityTemplate`) への依存を除去 (`CinematicSingletonBehaviour<T>` を同梱)
