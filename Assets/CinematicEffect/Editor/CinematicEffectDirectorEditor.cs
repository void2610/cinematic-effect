using UnityEditor;
using UnityEngine;

namespace Void2610.CinematicEffect.Editor
{
    /// <summary>
    /// <see cref="CinematicEffectDirector"/> のカスタムエディタ。
    /// テスト機能は <see cref="CinematicTestWindow"/> に委譲する。
    /// </summary>
    [CustomEditor(typeof(CinematicEffectDirector))]
    public class CinematicEffectDirectorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Cinematic Test Window を開く"))
            {
                CinematicTestWindow.Open((CinematicEffectDirector)target);
            }
        }
    }
}
