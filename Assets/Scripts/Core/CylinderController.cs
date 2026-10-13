using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CylinderController : MonoBehaviour
{
    [Header("UI表示用")]
    [SerializeField] private TextMeshProUGUI reloadInfoText;
    [SerializeField] private float displayDuration = 3.0f;     // 表示し続ける時間（秒）
    [SerializeField] private float fadeDuration = 0.5f;       // フェードイン・アウトにかかる時間（秒）

    [SerializeField] private List<bool> _cylinder = new();
    [SerializeField, Min(3)] private int _minAmmunition = 3;
    private int _maxAmmunition = 9;
    private float _minRate = 0.4f;
    private float _maxRate = 0.8f;

    private Coroutine _fadeRoutine;

    public int RemainingBullets => _cylinder.Count;

    private void Start()
    {
        // ゲーム開始時は完全に透明にして非表示（またはオブジェクト無効）にしておく
        if (reloadInfoText != null)
        {
            Color col = reloadInfoText.color;
            col.a = 0f;
            reloadInfoText.color = col;
            reloadInfoText.gameObject.SetActive(false);
        }
    }

    public void SetCylinder()
    {
        _cylinder.Clear();
        int random = Random.Range(_minAmmunition, _maxAmmunition);
        Vector2Int rate = new((int)(random * _minRate), (int)(random * _maxRate) + 1);
        int realCount = Random.Range(rate.x, rate.y);
        for (int c = 0; c < realCount; c++)
        {
            _cylinder.Add(true);
        }
        for (int c = 0; c < random - realCount; c++)
        {
            _cylinder.Add(false);
        }

        for (int r = _cylinder.Count - 1; r > 0; r--)
        {
            int randomIndex = Random.Range(0, r + 1);
            bool temp = _cylinder[r];
            _cylinder[r] = _cylinder[randomIndex];
            _cylinder[randomIndex] = temp;
        }

        string msg = "実弾は " + realCount + "発、空砲は " + (random - realCount) + "発だ";
        Debug.LogWarning(msg);

        ShowReloadInfo(msg);
    }

    private void ShowReloadInfo(string message)
    {
        if (reloadInfoText == null) return;

        reloadInfoText.text = message;
        reloadInfoText.gameObject.SetActive(true);

        // 既に動いているフェード処理があれば停止
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
        }

        // フェードインして、待機して、フェードアウトする一連のコルーチンを開始
        _fadeRoutine = StartCoroutine(FadeRoutine(displayDuration));
    }

    private IEnumerator FadeRoutine(float stayDuration)
    {
        // 1. フェードイン（透明 0 -> 不透明 1）
        yield return StartCoroutine(FadeTextAlpha(0f, 1f, fadeDuration));

        // 2. 指定時間そのまま待機
        yield return new WaitForSeconds(stayDuration);

        // 3. フェードアウト（不透明 1 -> 透明 0）
        yield return StartCoroutine(FadeTextAlpha(1f, 0f, fadeDuration));

        // 完全に消えたらオブジェクトを非効化
        reloadInfoText.gameObject.SetActive(false);
    }

    private IEnumerator FadeTextAlpha(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        Color color = reloadInfoText.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            reloadInfoText.color = color;
            yield return null;
        }

        color.a = endAlpha;
        reloadInfoText.color = color;
    }

    public bool GetShot()
    {
        bool shotType = _cylinder[0];
        _cylinder.RemoveAt(0);
        return shotType;
    }

    public int RemainingRealBullets
    {
        get
        {
            int count = 0;
            foreach (bool b in _cylinder) if (b) count++;
            return count;
        }
    }

    // アーティファクト & AI用メソッド ---
    public bool PeekNextBullet()
    {
        if (_cylinder.Count > 0) return _cylinder[0];
        return false;
    }

    public void ForceSetNextBulletReal()
    {
        if (_cylinder.Count > 0) _cylinder[0] = true;
    }
}