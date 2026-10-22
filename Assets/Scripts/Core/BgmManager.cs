using UnityEngine;
using UnityEngine.SceneManagement;

public class BgmManager : MonoBehaviour
{
    private static BgmManager _instance;
    public static BgmManager Instance => _instance;

    [Header("オーディオソース設定")]
    [SerializeField] private AudioSource _bgmAudioSource; // BGM用
    [SerializeField] private AudioSource _seAudioSource;  // SE用
    [SerializeField] private AudioLowPassFilter _bgmLowPassFilter; // 籠もり演出用フィルター

    [Header("BGM音量設定")]
    [SerializeField, Range(0f, 1f)] private float titleBgmVolume = 0.5f;   // タイトルでの音量
    [SerializeField, Range(0f, 1f)] private float inGameBgmVolume = 0.1f; // インゲームでの音量

    // 外部から現在のインゲームBGM音量を取得できるようにするプロパティ
    public float InGameBgmVolume => inGameBgmVolume;

    [Header("SE音量設定")]
    [SerializeField, Range(0f, 1f)] private float seMasterVolume = 1.0f;  // SE全体の音量調節用

    [Header("共通SE設定（銃声など）")]
    [SerializeField] private AudioClip realShotClip;  // 実弾用クリップ
    [SerializeField] private AudioClip blankShotClip; // 空砲用クリップ

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject); // シーンを跨いでも消さない

            if (_bgmAudioSource == null)
            {
                _bgmAudioSource = GetComponent<AudioSource>();
            }
            if (_bgmLowPassFilter == null)
            {
                _bgmLowPassFilter = GetComponent<AudioLowPassFilter>();
            }
        }
        else
        {
            Destroy(gameObject); // 重複防止
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_bgmAudioSource == null) return;

        if (scene.name == "InGameScene")
        {
            _bgmAudioSource.volume = inGameBgmVolume; // 0.1
            Debug.Log($"インゲームシーンへ移動：BGM音量を {inGameBgmVolume} に設定しました。");
        }
        else if (scene.name == "TitleScene")
        {
            _bgmAudioSource.volume = titleBgmVolume;
            Debug.Log($"タイトルシーンへ移動：BGM音量を {titleBgmVolume} に設定しました。");
        }

        if (_bgmLowPassFilter != null)
        {
            _bgmLowPassFilter.cutoffFrequency = 22000f;
        }
    }

    /// <summary>
    /// 登録済みの銃声SEを鳴らす（実弾 / 空砲）
    /// </summary>
    public void PlayGunshotSE(bool isRealShot)
    {
        AudioClip clipToPlay = isRealShot ? realShotClip : blankShotClip;
        PlaySE(clipToPlay);
    }

    /// <summary>
    /// 任意のSEをMaster音量で再生する共通メソッド
    /// </summary>
    public void PlaySE(AudioClip clip)
    {
        if (_seAudioSource != null && clip != null)
        {
            // Master音量が 0 なら再生しない
            if (seMasterVolume <= 0.001f) return;

            _seAudioSource.PlayOneShot(clip, seMasterVolume);
        }
    }

    /// <summary>
    /// BGMの音量とフィルターの周波数を直接設定する（演出用）
    /// </summary>
    public void SetBgmVolumeAndFilter(float volume, float cutoffFreq)
    {
        if (_bgmAudioSource != null)
        {
            _bgmAudioSource.volume = volume;
        }
        if (_bgmLowPassFilter != null)
        {
            _bgmLowPassFilter.cutoffFrequency = cutoffFreq;
        }
    }
}