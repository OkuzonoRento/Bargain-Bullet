using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    // タイトルのスタートボタンから呼び出す
    public void OnClickStartGame()
    {
        // "InGameScene" の部分は実際のインゲームシーン名に合わせて変更してください
        SceneManager.LoadScene("InGameScene");
    }
}