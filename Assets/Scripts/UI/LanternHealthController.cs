using System.Collections;
using UnityEngine;

public class LanternHealthController : MonoBehaviour
{
    [Header("1~5のランタンオブジェクトを順番に登録 (Lantern_1~5)")]
    [SerializeField] private GameObject[] lanterns = new GameObject[5];

    [Header("発光パラメータ設定")]
    [SerializeField, Tooltip("光っているときのGlowStrengthの値（例: 200）")]
    private float activeGlowStrength = 200f;
    [SerializeField, Tooltip("消灯しているときのGlowStrengthの値（例: 0）")]
    private float inactiveGlowStrength = 0f;

    [Header("マテリアルのインデックス設定")]
    [SerializeField, Tooltip("Lantern Glowマテリアルがあるスロット（Element 1なら 1）")]
    private int glowMaterialIndex = 1;

    [Header("アニメーション設定")]
    [SerializeField] private float transitionDuration = 0.3f; // 変幻にかかる時間

    private Coroutine[] _activeCoroutines = new Coroutine[5];

    /// <summary>
    /// 体力（1~5）に応じて、各ランタンのGlowStrengthを動的に変更する
    /// </summary>
    public void UpdateLanternGlow(int currentHp)
    {
        for (int i = 0; i < lanterns.Length; i++)
        {
            if (lanterns[i] == null) continue;

            bool shouldGlow = (i < currentHp);
            float targetStrength = shouldGlow ? activeGlowStrength : inactiveGlowStrength;

            Renderer rend = lanterns[i].GetComponent<Renderer>();
            if (rend != null)
            {
                // 個別のマテリアルインスタンスを取得（他のランタンと共有させないため）
                Material[] mats = rend.materials;
                if (mats.Length > glowMaterialIndex && mats[glowMaterialIndex] != null)
                {
                    // 進行中のアニメーションがあれば停止
                    if (_activeCoroutines[i] != null)
                    {
                        StopCoroutine(_activeCoroutines[i]);
                    }

                    // スムーズにGlowStrengthを変化させる
                    _activeCoroutines[i] = StartCoroutine(AnimateGlowStrength(mats[glowMaterialIndex], targetStrength, transitionDuration));
                }
            }
        }
    }

    /// <summary>
    /// GlowStrengthを滑らかに変化させるコルーチン
    /// </summary>
    private IEnumerator AnimateGlowStrength(Material mat, float targetStrength, float duration)
    {
        if (!mat.HasProperty("_GlowStrength")) yield break;

        float startStrength = mat.GetFloat("_GlowStrength");
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float current = Mathf.Lerp(startStrength, targetStrength, t);

            mat.SetFloat("_GlowStrength", current);
            yield return null;
        }

        mat.SetFloat("_GlowStrength", targetStrength);
    }
}