using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BattleCameraController : MonoBehaviour
{
    [Header("カメラ本体の参照")]
    [SerializeField] private Transform mainCameraTransform;   // 動かすカメラ（Main Camera等）
    [SerializeField, Range(0.05f, 0.5f)] private float transitionDuration = 0.25f; // カメラ切替時間

    [System.Serializable]
    public struct CameraViewData
    {
        public Vector3 position;
        public Vector3 eulerAngles;

        public void ApplyTo(Transform t)
        {
            if (t == null) return;
            t.position = position;
            t.rotation = Quaternion.Euler(eulerAngles);
        }
    }

    [Header("各視点の位置と角度")]
    [SerializeField] private CameraViewData defaultView = new CameraViewData { position = new Vector3(0, 2, -5), eulerAngles = new Vector3(15, 0, 0) };
    [SerializeField] private CameraViewData selfView = new CameraViewData { position = new Vector3(0, 1.4f, -1.8f), eulerAngles = new Vector3(12, 0, 0) };
    [SerializeField] private CameraViewData opponentView = new CameraViewData { position = new Vector3(0, 2.2f, -5.5f), eulerAngles = new Vector3(18, 180, 0) };

    [Header("リアルな演出設定")]
    [SerializeField] private bool enableBreathing = true;       // 呼吸のような微細な揺れ
    [SerializeField] private float breathAmount = 0.005f;       // 揺れの大きさ
    [SerializeField] private float breathSpeed = 2.0f;          // 揺れの速さ

    [Header("暗転演出用UI")]
    [SerializeField] private Image blackoutPanel;

    private Coroutine _cameraMoveRoutine;
    private Coroutine _recoilRoutine;
    private CameraViewData _currentTargetView;
    private bool _isMoving = false;

    // ★ 外部からカメラが移動中かどうかを確認するためのプロパティ
    public bool IsMoving => _isMoving;

    private void Start()
    {
        _currentTargetView = defaultView;
    }

    private void Update()
    {
        // カメラが移動中や反動中でなければ、呼吸の揺れを加える
        if (enableBreathing && !_isMoving && _recoilRoutine == null && mainCameraTransform != null)
        {
            float noiseX = Mathf.Sin(Time.time * breathSpeed) * breathAmount;
            float noiseY = Mathf.Cos(Time.time * (breathSpeed * 0.8f)) * breathAmount;

            Vector3 targetPos = _currentTargetView.position + new Vector3(noiseX, noiseY, 0f);
            mainCameraTransform.position = Vector3.Lerp(mainCameraTransform.position, targetPos, Time.deltaTime * 5f);
        }
    }

    /// <summary>
    /// 対象に応じたカメラワークの切り替え
    /// </summary>
    public void SwitchCameraForTarget(bool targetIsEnemy, bool isShootingSelf)
    {
        CameraViewData targetView = defaultView;

        if (isShootingSelf)
        {
            targetView = selfView;
        }
        else
        {
            targetView = targetIsEnemy ? opponentView : selfView;
        }

        _currentTargetView = targetView;

        if (mainCameraTransform != null)
        {
            if (_recoilRoutine != null) { StopCoroutine(_recoilRoutine); _recoilRoutine = null; }

            if (transitionDuration > 0f)
            {
                if (_cameraMoveRoutine != null) StopCoroutine(_cameraMoveRoutine);
                _cameraMoveRoutine = StartCoroutine(MoveCameraSmoothly(targetView.position, Quaternion.Euler(targetView.eulerAngles)));
            }
            else
            {
                targetView.ApplyTo(mainCameraTransform);
            }
        }
    }

    /// <summary>
    /// 通常視点に戻す
    /// </summary>
    public void ResetCamera()
    {
        _currentTargetView = defaultView;

        if (mainCameraTransform != null)
        {
            if (_recoilRoutine != null) { StopCoroutine(_recoilRoutine); _recoilRoutine = null; }

            if (transitionDuration > 0f)
            {
                if (_cameraMoveRoutine != null) StopCoroutine(_cameraMoveRoutine);
                _cameraMoveRoutine = StartCoroutine(MoveCameraSmoothly(defaultView.position, Quaternion.Euler(defaultView.eulerAngles)));
            }
            else
            {
                defaultView.ApplyTo(mainCameraTransform);
            }
        }
    }

    private IEnumerator MoveCameraSmoothly(Vector3 targetPos, Quaternion targetRot)
    {
        _isMoving = true;
        float elapsed = 0f;
        Vector3 startPos = mainCameraTransform.position;
        Quaternion startRot = mainCameraTransform.rotation;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            mainCameraTransform.position = Vector3.Lerp(startPos, targetPos, t);
            mainCameraTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        mainCameraTransform.position = targetPos;
        mainCameraTransform.rotation = targetRot;
        _isMoving = false;
    }

    /// <summary>
    /// 撃ったとき、または撃たれたときの強力な反動（視点は戻さず、その場で揺れたまま維持する）
    /// </summary>
    public void PlayRecoil(bool isHeavyHit = false)
    {
        if (_recoilRoutine != null) StopCoroutine(_recoilRoutine);
        _recoilRoutine = StartCoroutine(RecoilRoutine(isHeavyHit));
    }

    private IEnumerator RecoilRoutine(bool isHeavyHit)
    {
        if (mainCameraTransform == null) yield break;

        Quaternion startRot = mainCameraTransform.rotation;
        // 撃たれた時（被弾時）や強力な発砲時は反動を大きくする
        float pitchPunch = isHeavyHit ? -8f : -4f;
        float yawPunch = UnityEngine.Random.Range(-3f, 3f);

        Quaternion peakRot = startRot * Quaternion.Euler(pitchPunch, yawPunch, UnityEngine.Random.Range(-2f, 2f));

        float elapsed = 0f;
        float punchDuration = 0.08f; // 一気に跳ね上がる時間

        // 1. 瞬時にガクッと跳ね上がる
        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / punchDuration;
            mainCameraTransform.rotation = Quaternion.Slerp(startRot, peakRot, t);
            yield return null;
        }

        mainCameraTransform.rotation = peakRot;
        _recoilRoutine = null;
    }

    /// <summary>
    /// 自分に実弾が当たったときのパートナー・真っ暗になってから急に目を覚ます演出
    /// </summary>
    public IEnumerator PlayBlackoutWakeUpRoutine()
    {
        if (blackoutPanel == null) yield break;

        // 被弾時の大きな反動もあわせて発生させる
        PlayRecoil(isHeavyHit: true);

        // 1. 瞬間的に画面を真っ暗にする
        Color col = blackoutPanel.color;
        col.a = 1f;
        blackoutPanel.color = col;
        blackoutPanel.gameObject.SetActive(true);

        // 2. 暗闇の中で少し待つ（気絶している時間）
        yield return new WaitForSeconds(0.6f);

        // 3. 急に目を覚ますようにスッと視界が開ける
        float elapsed = 0f;
        float duration = 0.25f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.Sin(t * Mathf.PI * 0.5f);
            col.a = Mathf.Lerp(1f, 0f, t);
            blackoutPanel.color = col;
            yield return null;
        }

        col.a = 0f;
        blackoutPanel.color = col;
        blackoutPanel.gameObject.SetActive(false);
    }
}