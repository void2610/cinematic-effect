using System;
using System.Collections.Generic;

namespace Void2610.CinematicEffect
{
    /// <summary>
    /// 映画的演出の手順を記述する純粋なデータクラス。
    /// エフェクトのインスタンスを持たず、型情報と操作種別のみを保持する。
    /// 実行は <see cref="CinematicDirector.RunAsync"/> が担当する。
    /// </summary>
    public sealed class CinematicSequence
    {
        /// <summary>ステップの操作種別。</summary>
        public enum StepKind
        {
            /// <summary>Fire-and-Forget で開始。</summary>
            Play,

            /// <summary>完了を待機して開始。</summary>
            PlayAndAwait,

            /// <summary>停止を待機。</summary>
            Stop,

            /// <summary>指定秒数だけ待機。</summary>
            Delay,
        }

        /// <summary>シーケンスの1ステップ。record struct は C# 10 構文で Unity (C# 9) でコンパイルできないため使わない。</summary>
        internal readonly struct Step
        {
            public StepKind Kind { get; }
            public Type EffectType { get; }
            public float DelaySeconds { get; }
            public CinematicEffectConfig Config { get; }

            public Step(StepKind kind, Type effectType = null, float delaySeconds = 0f, CinematicEffectConfig config = null)
            {
                Kind = kind;
                EffectType = effectType;
                DelaySeconds = delaySeconds;
                Config = config;
            }
        }

        private readonly List<Step> _steps = new();

        private CinematicSequence() { }

        /// <summary>新しいシーケンスを生成する。</summary>
        public static CinematicSequence Create() => new();

        /// <summary>演出を Fire-and-Forget で開始する（ループ型エフェクト向け）。</summary>
        public CinematicSequence Play<T>(CinematicEffectConfig config = null) where T : class, ICinematicEffect
        {
            _steps.Add(new Step(StepKind.Play, typeof(T), config: config));
            return this;
        }

        /// <summary>演出を開始し、完了を待機する（1回完結型エフェクト向け）。</summary>
        public CinematicSequence PlayAndAwait<T>(CinematicEffectConfig config = null) where T : class, ICinematicEffect
        {
            _steps.Add(new Step(StepKind.PlayAndAwait, typeof(T), config: config));
            return this;
        }

        /// <summary>演出を停止し、停止アニメーションの完了を待機する。</summary>
        public CinematicSequence Stop<T>(CinematicEffectConfig config = null) where T : class, ICinematicEffect
        {
            _steps.Add(new Step(StepKind.Stop, typeof(T), config: config));
            return this;
        }

        /// <summary>演出を Fire-and-Forget で開始する（ランタイム型解決用）。</summary>
        public CinematicSequence Play(Type effectType, CinematicEffectConfig config = null)
        {
            _steps.Add(new Step(StepKind.Play, effectType, config: config));
            return this;
        }

        /// <summary>演出を開始し、完了を待機する（ランタイム型解決用）。</summary>
        public CinematicSequence PlayAndAwait(Type effectType, CinematicEffectConfig config = null)
        {
            _steps.Add(new Step(StepKind.PlayAndAwait, effectType, config: config));
            return this;
        }

        /// <summary>演出を停止し、停止アニメーションの完了を待機する（ランタイム型解決用）。</summary>
        public CinematicSequence Stop(Type effectType, CinematicEffectConfig config = null)
        {
            _steps.Add(new Step(StepKind.Stop, effectType, config: config));
            return this;
        }

        /// <summary>指定秒数だけ待機する。</summary>
        public CinematicSequence Delay(float seconds)
        {
            _steps.Add(new Step(StepKind.Delay, delaySeconds: seconds));
            return this;
        }

        /// <summary><see cref="CinematicDirector"/> がステップを読み取るための公開プロパティ。</summary>
        internal IReadOnlyList<Step> Steps => _steps;
    }
}
