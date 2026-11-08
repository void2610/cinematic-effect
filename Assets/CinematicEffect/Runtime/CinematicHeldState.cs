using System;
using System.Collections.Generic;

namespace Void2610.CinematicEffect
{
    /// <summary>
    /// シーケンスを順に実行した結果として、最後まで保持されている演出だけを求める
    /// 途中経過を見せずに状態を再現したいとき (セーブからの復帰の早送り等) に使う
    /// </summary>
    public sealed class CinematicHeldState
    {
        // 適用順を保つため Dictionary でなく List で持つ
        private readonly List<(Type EffectType, CinematicEffectConfig Config)> _held = new();

        /// <summary>保持中の演出が 1 つも無いか</summary>
        public bool IsEmpty => _held.Count == 0;

        /// <summary>シーケンスを再生したものとして保持状態を更新する</summary>
        public void Apply(CinematicSequence sequence)
        {
            foreach (var step in sequence.Steps)
            {
                if (step.Kind == CinematicSequence.StepKind.Delay || step.EffectType == null) continue;
                _held.RemoveAll(held => held.EffectType == step.EffectType);
                // 完了を待つ演出は一度きりなので、Play だけが保持として残る
                if (step.Kind == CinematicSequence.StepKind.Play) _held.Add((step.EffectType, step.Config));
            }
        }

        /// <summary>保持中の演出をまとめて開始するシーケンスを作る</summary>
        public CinematicSequence BuildPlaySequence()
        {
            var sequence = CinematicSequence.Create();
            foreach (var (effectType, config) in _held) sequence.Play(effectType, config);
            return sequence;
        }

        /// <summary>保持状態を空にする</summary>
        public void Clear() => _held.Clear();
    }
}
