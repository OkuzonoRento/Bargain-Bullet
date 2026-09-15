using UnityEngine;
using UnityEditor; // ※Editorスクリプトでのみ使用可能

[CustomEditor(typeof(BattleManager))]
public class TargetScriptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 元のインスペクター表示（変数など）をそのまま描画
        DrawDefaultInspector();

        // 描画対象のオブジェクトを参照
        BattleManager battleManagerScript = (BattleManager)target;

        if (GUILayout.Button("自分に向けて撃つ"))
        {
            battleManagerScript.OnPlayerShootSelf();
        }

        if (GUILayout.Button("相手に向けて撃つ"))
        {
            battleManagerScript.OnPlayerShootOpponent();
        }
    }
}