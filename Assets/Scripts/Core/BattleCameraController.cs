using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // URP用

public class BattleCameraController : MonoBehaviour
{
    [Header("カメラ本体の参照")]
    [SerializeField] private Transform mainCameraTransform;

    [Header("カメラ切替・復帰の速度")]
    [SerializeField, Range(0.1f, 2.0f)] private float transitionDuration = 1.0f;

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
    [SerializeField] private bool enableBreathing = true;
    [SerializeField] private float breathAmount = 0.005f;
    [SerializeField] private float breathSpeed = 2.0f;

    [Header("暗転・ピント演出用 URP Volume の設定")]
    [SerializeField] private Volume blackoutVolume;
    private ColorAdjustments _colorAdjustments;
    private DepthOfField _depthOfField;

    private Coroutine _cameraMoveRoutine;
    private Coroutine _recoilRoutine;
    private CameraViewData _currentTargetView;
    private bool _isMoving = false;

    public bool IsMoving => _isMoving;

    private void Start()
    {
        _currentTargetView = defaultView;

        if (blackoutVolume != null && blackoutVolume.profile != null)
        {
            blackoutVolume.profile.TryGet(out _colorAdjustments);
            blackoutVolume.profile.TryGet(out _depthOfField);
        }
    }

    private void Update()
    {
        if (enableBreathing && !_isMoving && _recoilRoutine == null && mainCameraTransform != null)
        {
            float noiseX = Mathf.Sin(Time.time * breathSpeed) * breathAmount;
            float noiseY = Mathf.Cos(Time.time * (breathSpeed * 0.8f)) * breathAmount;

            Vector3 targetPos = _currentTargetView.position + new Vector3(noiseX, noiseY, 0f);
            mainCameraTransform.position = Vector3.Lerp(mainCameraTransform.position, targetPos, Time.deltaTime * 5f);
        }
    }

    public void SwitchCameraForTarget(bool targetIsEnemy, bool isShootingSelf)
    {
        CameraViewData targetView = isShootingSelf ? selfView : (targetIsEnemy ? opponentView : selfView);
        _currentTargetView = targetView;

        if (mainCameraTransform != null)
        {
            if (_recoilRoutine != null) { StopCoroutine(_recoilRoutine); _recoilRoutine = null; }

            if (transitionDuration > 0f)
            {
                if (_cameraMoveRoutine != null) StopCoroutine(_cameraMoveRoutine);
                _cameraMoveRoutine = StartCoroutine(MoveCameraSmoothly(targetView.position, Quaternion.Euler(targetView.eulerAngles), transitionDuration));
            }
            else
            {
                targetView.ApplyTo(mainCameraTransform);
            }
        }
    }

    public void ResetCamera()
    {
        _currentTargetView = defaultView;

        if (mainCameraTransform != null)
        {
            if (_recoilRoutine != null) { StopCoroutine(_recoilRoutine); _recoilRoutine = null; }

            if (transitionDuration > 0f)
            {
                if (_cameraMoveRoutine != null) StopCoroutine(_cameraMoveRoutine);
                _cameraMoveRoutine = StartCoroutine(MoveCameraSmoothly(defaultView.position, Quaternion.Euler(defaultView.eulerAngles), transitionDuration));
            }
            else
            {
                defaultView.ApplyTo(mainCameraTransform);
            }
        }
    }

    private IEnumerator MoveCameraSmoothly(Vector3 targetPos, Quaternion targetRot, float duration)
    {
        _isMoving = true;
        float elapsed = 0f;
        Vector3 startPos = mainCameraTransform.position;
        Quaternion startRot = mainCameraTransform.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            mainCameraTransform.position = Vector3.Lerp(startPos, targetPos, t);
            mainCameraTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        mainCameraTransform.position = targetPos;
        mainCameraTransform.rotation = targetRot;
        _isMoving = false;
    }

    public void PlayRecoil(bool isHeavyHit = false)
    {
        if (_recoilRoutine != null) StopCoroutine(_recoilRoutine);
        _recoilRoutine = StartCoroutine(RecoilRoutine(isHeavyHit));
    }

    private IEnumerator RecoilRoutine(bool isHeavyHit)
    {
        if (mainCameraTransform == null) yield break;

        Quaternion startRot = mainCameraTransform.rotation;
        float pitchPunch = isHeavyHit ? -8f : -4f;
        float yawPunch = UnityEngine.Random.Range(-3f, 3f);
        Quaternion peakRot = startRot * Quaternion.Euler(pitchPunch, yawPunch, UnityEngine.Random.Range(-2f, 2f));

        float elapsed = 0f;
        float punchDuration = 0.08f;

        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            mainCameraTransform.rotation = Quaternion.Slerp(startRot, peakRot, elapsed / punchDuration);
            yield return null;
        }

        mainCameraTransform.rotation = peakRot;
        _recoilRoutine = null;
    }

    /// <summary>
    /// BgmManager 側の共通SE設定を参照して銃声を鳴らす
    /// </summary>
    public void PlayGunshot(bool isRealShot)
    {
        if (BgmManager.Instance != null)
        {
            BgmManager.Instance.PlayGunshotSE(isRealShot);
        }
        else
        {
            Debug.LogWarning("BgmManagerが存在しないため、銃声を再生できません。");
        }
    }

    /// <summary>
    /// 暗転 ➔ ピントが合ってから、ゆっくりとカメラ位置をデフォルトに戻す演出
    /// </summary>
    public IEnumerator PlayBlackoutWakeUpRoutine()
    {
        PlayRecoil(isHeavyHit: true);

        // -------------------------------------------------------------
        // Step 1: 一瞬で完全な黒にする（短めの暗転）
        // -------------------------------------------------------------
        if (_colorAdjustments != null)
        {
            _colorAdjustments.postExposure.overrideState = true;
            _colorAdjustments.postExposure.value = -20f;
        }

        if (_depthOfField != null)
        {
            _depthOfField.active = true;
            _depthOfField.focusDistance.overrideState = true;
            _depthOfField.focusDistance.value = 0.1f;
        }

        // BgmManager経由でBGMを完全遮断＆こもらせる
        if (BgmManager.Instance != null)
        {
            BgmManager.Instance.SetBgmVolumeAndFilter(0f, 400f);
        }

        yield return new WaitForSeconds(0.4f);

        // -------------------------------------------------------------
        // Step 2: 目が覚めて、明るくなりながらピントが合っていく
        // -------------------------------------------------------------
        float wakeUpDuration = 1.2f;
        float elapsed = 0f;

        // BgmManagerから現在の設定音量を安全に取得して大音量演出に利用
        float defaultBgmVol = (BgmManager.Instance != null) ? BgmManager.Instance.InGameBgmVolume : 0.1f;
        float loudBgmVolume = defaultBgmVol * 1.5f;

        if (BgmManager.Instance != null)
        {
            BgmManager.Instance.SetBgmVolumeAndFilter(loudBgmVolume, 400f);
        }

        float startExposure = -20f;
        float targetExposure = 0f;

        float startFocus = 0.1f;
        float targetFocus = 10f;

        while (elapsed < wakeUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / wakeUpDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (_colorAdjustments != null)
            {
                _colorAdjustments.postExposure.value = Mathf.Lerp(startExposure, targetExposure, smoothT);
            }

            if (_depthOfField != null)
            {
                _depthOfField.focusDistance.value = Mathf.Lerp(startFocus, targetFocus, smoothT);
            }

            float currentVol = Mathf.Lerp(loudBgmVolume, defaultBgmVol, smoothT);
            float currentCutoff = Mathf.Lerp(400f, 22000f, smoothT);

            if (BgmManager.Instance != null)
            {
                BgmManager.Instance.SetBgmVolumeAndFilter(currentVol, currentCutoff);
            }

            yield return null;
        }

        // 完全にピントが合い、明るくなった状態を確定
        if (_colorAdjustments != null) _colorAdjustments.postExposure.value = targetExposure;
        if (_depthOfField != null) _depthOfField.focusDistance.value = targetFocus;

        if (BgmManager.Instance != null)
        {
            BgmManager.Instance.SetBgmVolumeAndFilter(defaultBgmVol, 22000f);
        }

        // -------------------------------------------------------------
        // Step 3: ピントが完全に合ったあとに、カメラの位置をゆっくり戻す
        // -------------------------------------------------------------
        _currentTargetView = defaultView;

        if (mainCameraTransform != null)
        {
            if (_recoilRoutine != null) { StopCoroutine(_recoilRoutine); _recoilRoutine = null; }

            if (_cameraMoveRoutine != null) StopCoroutine(_cameraMoveRoutine);

            float returnDuration = 1.2f;
            _cameraMoveRoutine = StartCoroutine(MoveCameraSmoothly(defaultView.position, Quaternion.Euler(defaultView.eulerAngles), returnDuration));
        }

        while (_isMoving)
        {
            yield return null;
        }
    }

    /// <summary>
    /// リロードやアニメーション切り替わりの瞬間に、まばたきするように一瞬だけ暗転して戻る演出
    /// </summary>
    public IEnumerator PlayBlinkRoutine()
    {
        // 1. 瞬間的に露出を下げる
        if (_colorAdjustments != null)
        {
            _colorAdjustments.postExposure.overrideState = true;
            _colorAdjustments.postExposure.value = -20f;
        }

        yield return new WaitForSeconds(0.15f);

        // 2. スッと視界が戻る
        float duration = 0.2f;
        float elapsed = 0f;
        float startExposure = -20f;
        float targetExposure = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (_colorAdjustments != null)
            {
                _colorAdjustments.postExposure.value = Mathf.Lerp(startExposure, targetExposure, smoothT);
            }
            yield return null;
        }

        if (_colorAdjustments != null)
        {
            _colorAdjustments.postExposure.value = targetExposure;
        }
    }
}