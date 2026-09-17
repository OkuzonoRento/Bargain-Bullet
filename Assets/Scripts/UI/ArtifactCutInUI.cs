using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace BargainBullet.UI
{
    public class ArtifactCutInUI : MonoBehaviour
    {
        public static ArtifactCutInUI Instance { get; private set; }

        [Header("UIパーツ参照")]
        [SerializeField] private GameObject _cutInPanel;
        [SerializeField] private TextMeshProUGUI _artifactNameText;
        [SerializeField] private TextMeshProUGUI _comboCountText; // コンボ数表示用
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("3Dモデル演出設定")]
        [SerializeField] private Transform _modelSpawnAnchor;
        [SerializeField] private float _rotationSpeed = 360f; // 高速回転

        private readonly Queue<(string name, GameObject prefab)> _cutInQueue = new Queue<(string, GameObject)>();
        private bool _isProcessingQueue = false;
        private GameObject _currentModelInstance;
        private int _currentComboCount = 0;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (_cutInPanel != null) _cutInPanel.SetActive(false);
        }

        /// <summary>
        /// 演出リクエストをキューに追加
        /// </summary>
        public void EnqueueCutIn(string artifactName, GameObject artifactPrefab)
        {
            _cutInQueue.Enqueue((artifactName, artifactPrefab));
            if (!_isProcessingQueue)
            {
                StartCoroutine(ProcessQueueRoutine());
            }
        }

        private IEnumerator ProcessQueueRoutine()
        {
            _isProcessingQueue = true;
            _currentComboCount = 0;
            if (_cutInPanel != null) _cutInPanel.SetActive(true);

            while (_cutInQueue.Count > 0)
            {
                var (name, prefab) = _cutInQueue.Dequeue();
                _currentComboCount++;

                // 表示更新
                if (_artifactNameText != null) _artifactNameText.text = name;
                if (_comboCountText != null)
                {
                    _comboCountText.text = _currentComboCount > 1 ? $"{_currentComboCount} COMBO!" : "";
                }

                // モデル生成
                if (_currentModelInstance != null) Destroy(_currentModelInstance);
                if (prefab != null && _modelSpawnAnchor != null)
                {
                    _currentModelInstance = Instantiate(prefab, _modelSpawnAnchor);
                    _currentModelInstance.transform.localPosition = Vector3.zero;
                    _currentModelInstance.transform.localRotation = Quaternion.identity;
                    _currentModelInstance.transform.localScale = Vector3.one;
                }

                // テンポよく表示（フェード＆高速回転：約0.35秒）
                float duration = 0.35f;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    if (_canvasGroup != null)
                    {
                        // 前半でフェードイン、後半で少し薄くして次へ繋ぐ
                        _canvasGroup.alpha = elapsed < 0.1f ? elapsed / 0.1f : 1f;
                    }
                    if (_currentModelInstance != null)
                    {
                        _currentModelInstance.transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime, Space.World);
                    }
                    yield return null;
                }
            }

            // キューが空になったら終了処理
            if (_currentModelInstance != null) Destroy(_currentModelInstance);
            if (_cutInPanel != null) _cutInPanel.SetActive(false);
            _isProcessingQueue = false;
        }
    }
}