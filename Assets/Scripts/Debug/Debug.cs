using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomEditor(typeof(BattleManager))]
public class TargetScriptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // インスペクターの標準変数を表示
        DrawDefaultInspector();

        BattleManager battleManagerScript = (BattleManager)target;

        GUILayout.Space(10);

        // プレイヤーの行動用テストボタン
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
#endif