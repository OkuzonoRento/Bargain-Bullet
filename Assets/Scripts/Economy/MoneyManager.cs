using UnityEngine;

namespace BargainBullet.Economy
{
    public class MoneyManager : MonoBehaviour
    {
        public static MoneyManager Instance { get; private set; }

        [Header("所持金 & 利息設定")]
        [SerializeField] private float _currentMoney = 100f;
        [SerializeField] private float _baseInterestRate = 0.10f; // 基本利息率 10%

        private bool _hasSeizedInterest = false; // ART_012用：利息激減フラグ

        public float CurrentMoney => _currentMoney;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// ART_013: 自分実弾被弾時の保険金獲得
        /// </summary>
        public void AddInsuranceMoney(float amount)
        {
            _currentMoney += amount;
            Debug.Log($"[MoneyManager] 保険金獲得: +${amount} (現在所持金: ${_currentMoney})");
        }

        /// <summary>
        /// ART_012: 致死耐え発動時の次回利息激減フラグ設定
        /// </summary>
        public void SetSeizedInterestFlag()
        {
            _hasSeizedInterest = true;
            Debug.LogWarning("[MoneyManager] 利息激減フラグがセットされました");
        }

        /// <summary>
        /// ART_009 / ART_012: 勝利時の利息計算
        /// </summary>
        public float CalculateWinReward(bool hasBonusInterestArtifact)
        {
            float rate = _baseInterestRate;

            // ART_009: 勝利時の利息率 +5%
            if (hasBonusInterestArtifact)
            {
                rate += 0.05f;
                Debug.Log("[MoneyManager] ファンファーレ効果: 利息率+5%");
            }

            // ART_012: 保険発動時は利息激減 (1/10に低減)
            if (_hasSeizedInterest)
            {
                rate *= 0.1f;
                _hasSeizedInterest = false; // フラグ消費
                Debug.LogWarning("[MoneyManager] 逆転の保険適用: 利息が激減しました");
            }

            float interestEarned = _currentMoney * rate;
            _currentMoney += interestEarned;

            Debug.Log($"[MoneyManager] 戦利品獲得！ 利息: +${interestEarned} (合計: ${_currentMoney})");
            return interestEarned;
        }
    }
}