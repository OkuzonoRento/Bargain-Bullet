using UnityEngine;
using UnityEditor; // ※Editorスクリプトでのみ使用可能

[CustomEditor(typeof(CylinderController))] // ① 対象となるスクリプトを指定
public class TargetScriptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 元のインスペクター表示（変数など）をそのまま描画
        DrawDefaultInspector();

        // 描画対象のオブジェクトを参照
        CylinderController script = (CylinderController)target;

        // ② インスペクター上にボタンを作成
        if (GUILayout.Button("シリンダー再装填"))
        {
            // ボタンが押された時の処理（メソッド実行）
            script.SetCylinder();
        }
    }
}