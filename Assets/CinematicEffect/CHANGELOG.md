# Changelog

## [Unreleased]

### Added

- `ICinematicEffect.IsPlayingChanged` / `CinematicEffectDirector.EffectPlayingChanged` を追加。
  再生状態をポーリングせずに追えるようにするためのイベント

## [0.1.0] - 2026-07-19

### Added

- my-unity-utils の `CinematicEffect/` を独立 UPM パッケージとして切り出し
- 全クラスを `Void2610.CinematicEffect` 名前空間へ移動
- my-unity-utils (`Void2610.UnityTemplate`) への依存を除去 (`CinematicSingletonBehaviour<T>` を同梱)
