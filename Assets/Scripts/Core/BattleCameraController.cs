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

    [Header("BGM・オーディオ演出設定")]
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioLowPassFilter bgmLowPassFilter;
    private float _defaultBgmVolume = 1.0f;

    // クリップごとに個別の音量倍率を持たせるためのデータ構造
    [Serializable]
    public struct SoundData
    {
        public AudioClip clip;
        [Range(0f, 2f)] public float volumeMultiplier; // このクリップ専用の音量倍率
    }

    [Header("登録された銃声クリップと音量設定")]
    [SerializeField] private SoundData realShotSound;  // 実弾用
    [SerializeField] private SoundData blankShotSound; // 空砲用

    [Header("暗転・ピント演出用 URP Volume の設定")]
    [SerializeField] private Volume blackoutVolume;
    private ColorAdjustments _colorAdjustments;
    private DepthOfField _depthOfField;

    private Coroutine _cameraMoveRoutine;
    private Coroutine _recoilRoutine;
    private CameraViewData _currentTargetView;
    private bool _isMoving = false;

    // 音量を確実に反映させるための内部用AudioSource
    private AudioSource _sfxInternalSource;

    public bool IsMoving => _isMoving;

    private void Start()
    {
        _currentTargetView = defaultView;

        if (bgmAudioSource != null)
        {
            _defaultBgmVolume = bgmAudioSource.volume;
        }

        if (blackoutVolume != null && blackoutVolume.profile != null)
        {
            blackoutVolume.profile.TryGet(out _colorAdjustments);
            blackoutVolume.profile.TryGet(out _depthOfField);
        }

        // スクリプト側で自動的にSE用のAudioSourceをアタッチ（手動アタッチ不要）
        _sfxInternalSource = gameObject.GetComponent<AudioSource>();
        if (_sfxInternalSource == null)
        {
            _sfxInternalSource = gameObject.AddComponent<AudioSource>();
        }
        _sfxInternalSource.spatialBlend = 0f; // 2Dサウンド（空間減衰なしで指定した音量を100%正確に出力）
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
    /// 設定されたクリップとVolume Multiplierの音量を100%確実に反映して銃声を鳴らす
    /// </summary>
    public void PlayGunshot(bool isRealShot)
    {
        SoundData targetSound = isRealShot ? realShotSound : blankShotSound;

        if (targetSound.clip != null)
        {
            if (_sfxInternalSource == null)
            {
                _sfxInternalSource = gameObject.GetComponent<AudioSource>();
                if (_sfxInternalSource == null) _sfxInternalSource = gameObject.AddComponent<AudioSource>();
            }

            // PlayOneShotで指定した倍率（0.01など）の音量を確実に適用して再生
            _sfxInternalSource.PlayOneShot(targetSound.clip, targetSound.volumeMultiplier);
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

        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = 0f;
            if (bgmLowPassFilter != null) bgmLowPassFilter.cutoffFrequency = 400f;
        }

        yield return new WaitForSeconds(0.4f);

        // -------------------------------------------------------------
        // Step 2: 目が覚めて、明るくなりながらピントが合っていく
        // -------------------------------------------------------------
        float wakeUpDuration = 1.2f;
        float elapsed = 0f;
        float loudBgmVolume = _defaultBgmVolume * 1.5f;

        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = loudBgmVolume;
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

            if (bgmAudioSource != null)
            {
                bgmAudioSource.volume = Mathf.Lerp(loudBgmVolume, _defaultBgmVolume, smoothT);

                if (bgmLowPassFilter != null)
                {
                    bgmLowPassFilter.cutoffFrequency = Mathf.Lerp(400f, 22000f, smoothT);
                }
            }

            yield return null;
        }

        // 完全にピントが合い、明るくなった状態を確定
        if (_colorAdjustments != null) _colorAdjustments.postExposure.value = targetExposure;
        if (_depthOfField != null) _depthOfField.focusDistance.value = targetFocus;

        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = _defaultBgmVolume;
        }
        if (bgmLowPassFilter != null)
        {
            bgmLowPassFilter.cutoffFrequency = 22000f;
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
}