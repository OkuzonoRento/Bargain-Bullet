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

    // メッセージの順番待ちキュー
    private readonly Queue<string> _messageQueue = new Queue<string>();
    private bool _isShowingMessage = false;

    public int RemainingBullets => _cylinder.Count;

    private void Start()
    {
        if (reloadInfoText != null)
        {
            Color col = reloadInfoText.color;
            col.a = 0f;
            reloadInfoText.color = col;
            //reloadInfoText.gameObject.SetActive(false);
        }
    }

    public void ShowTextInfo(string message)
    {
        // 順番待ちのキューに追加
        _messageQueue.Enqueue(message);

        // もし現在メッセージを表示中でなければ、すぐに再生を開始する
        if (!_isShowingMessage)
        {
            StartCoroutine(ProcessMessageQueue());
        }
    }

    private IEnumerator ProcessMessageQueue()
    {
        _isShowingMessage = true;

        while (_messageQueue.Count > 0)
        {
            string message = _messageQueue.Dequeue();
            yield return StartCoroutine(ShowMessageRoutine(message));
        }

        _isShowingMessage = false;
    }

    private IEnumerator ShowMessageRoutine(string message)
    {
        if (reloadInfoText == null) yield break;

        reloadInfoText.text = message;
        reloadInfoText.gameObject.SetActive(true);

        // 1. フェードイン
        yield return StartCoroutine(FadeTextAlpha(0f, 1f, fadeDuration));

        // 2. 待機
        yield return new WaitForSeconds(displayDuration);

        // 3. フェードアウト
        yield return StartCoroutine(FadeTextAlpha(1f, 0f, fadeDuration));

        reloadInfoText.gameObject.SetActive(false);
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

        ShowTextInfo(msg); // キューを経由して表示
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