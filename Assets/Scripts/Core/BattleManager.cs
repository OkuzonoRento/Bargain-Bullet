using BargainBullet.Artifacts;
using BargainBullet.Core;
using BargainBullet.Economy;
using System.Collections;
using UnityEngine;

public enum BattleTurn { Player, Enemy }

public class BattleManager : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private CylinderController _cylinderController;
    [SerializeField] private BattleAI _battleAI;
    [SerializeField] private ArtifactManager _artifactManager;

    [Header("ステータス")]
    [SerializeField, Min(2)] private int _playerHp = 2;
    [SerializeField, Min(2)] private int _enemyHp = 2;
    private int _maxHp;
    private int _consecutiveBlanks = 0;

    [Header("状態")]
    [SerializeField] private BattleTurn _currentTurn = BattleTurn.Player;

    private void Start()
    {
        if (_artifactManager != null)
        {
            // ART_015: 焼失ダメージを敵に適用するイベント購読
            _artifactManager.OnBurnOutDamageToEnemy += amount => ApplyDamageToEnemy(amount);
        }
        BattleSetup();
    }

    public void BattleSetup()
    {
        _maxHp = Random.Range(2, 6);
        _playerHp = _maxHp;
        _enemyHp = _maxHp;
        _consecutiveBlanks = 0;

        if (_cylinderController != null) _cylinderController.SetCylinder();

        _currentTurn = BattleTurn.Player;
        NotifyTurnStart();
    }

    #region Action Endpoints

    public void OnPlayerShootSelf()
    {
        if (_currentTurn == BattleTurn.Player)
            ExecuteShoot(targetIsEnemy: false, nextTurnIfHit: BattleTurn.Enemy, nextTurnIfMiss: BattleTurn.Player);
    }

    public void OnPlayerShootOpponent()
    {
        if (_currentTurn == BattleTurn.Player)
            ExecuteShoot(targetIsEnemy: true, nextTurnIfHit: BattleTurn.Enemy, nextTurnIfMiss: BattleTurn.Enemy);
    }

    public void OnEnemyShootSelf()
    {
        if (_currentTurn == BattleTurn.Enemy)
            ExecuteShoot(targetIsEnemy: true, nextTurnIfHit: BattleTurn.Player, nextTurnIfMiss: BattleTurn.Enemy);
    }

    public void OnEnemyShootOpponent()
    {
        if (_currentTurn == BattleTurn.Enemy)
            ExecuteShoot(targetIsEnemy: false, nextTurnIfHit: BattleTurn.Player, nextTurnIfMiss: BattleTurn.Player);
    }

    #endregion

    private void ExecuteShoot(bool targetIsEnemy, BattleTurn nextTurnIfHit, BattleTurn nextTurnIfMiss)
    {
        // =========================================================
        // 1. 【射撃前トリガー】引き金を引き切る・弾が出る前の処理
        // =========================================================
        if (_artifactManager != null)
        {
            // 射撃前カウントダウン（身代わり時計など）
            int dueDamage = _artifactManager.OnShotExecuted();
            if (dueDamage > 0)
            {
                ApplyDirectDamageToPlayer(dueDamage);
                if (_playerHp <= 0) return;
            }

            // 射撃前不発化判定 (ジャム)
            if (_currentTurn == BattleTurn.Player && !targetIsEnemy)
            {
                if (_artifactManager.CheckJamSelfReal())
                {
                    Debug.Log("[Artifact] ジャム発動！ 射撃前に無効化してターン継続！");
                    ChangeTurn(BattleTurn.Player);
                    return;
                }
            }
        }

        // 実際の着弾・発砲処理へ
        bool isReal = _cylinderController.GetShot();

        if (isReal)
        {
            _consecutiveBlanks = 0;
            int damage = 1;

            if (_currentTurn == BattleTurn.Player && targetIsEnemy && _artifactManager != null)
            {
                damage = _artifactManager.ProcessOutgoingDamage(damage);
            }

            if (targetIsEnemy) ApplyDamageToEnemy(damage);
            else ApplyDamageToPlayer(damage); // 被弾処理へ呼び出し

            if (_playerHp > 0 && _enemyHp > 0) ChangeTurn(nextTurnIfHit);
        }
        else
        {
            // 空砲処理...
            _consecutiveBlanks++;
            if (_currentTurn == BattleTurn.Player && !targetIsEnemy && _artifactManager != null)
            {
                _artifactManager.CheckCoolAdjacentOnSelfBlank();
                int heal = _artifactManager.CheckHealOnSelfBlank();
                if (heal > 0) _playerHp = Mathf.Min(_maxHp, _playerHp + heal);
            }
            ChangeTurn(nextTurnIfMiss);
        }
    }

    /// <summary>
    /// ★ 追加: プレイヤーへのダメージ適用処理（反射判定や被弾時エフェクトを経由）
    /// </summary>
    private void ApplyDamageToPlayer(int baseAmount)
    {
        int finalDamage = baseAmount;

        if (_artifactManager != null)
        {
            // 被弾時・反射判定 (ART_001 / ART_005)
            var (damage, reflect) = _artifactManager.ProcessIncomingDamage(baseAmount);
            finalDamage = damage;

            if (reflect > 0)
            {
                Debug.Log($"[Artifact] 被弾ダメージ {reflect} を相手に反射！");
                ApplyDamageToEnemy(reflect);
            }
        }

        if (finalDamage > 0)
        {
            ApplyDirectDamageToPlayer(finalDamage);
        }
    }

    private void ApplyDirectDamageToPlayer(int amount)
    {
        // =========================================================
        // 2. 【食らった瞬間・致死判定トリガー】
        // =========================================================
        bool isFatal = (_playerHp - amount) <= 0;

        // 食らった瞬間に耐える判定（食らった直後にカットインが入る）
        if (isFatal && _artifactManager != null && _artifactManager.CheckFatalProtection())
        {
            Debug.Log("[Artifact] 致死ダメージを受けた瞬間に耐えた！");
            _playerHp = 1;
        }
        else
        {
            _playerHp = Mathf.Max(0, _playerHp - amount);
        }

        if (_playerHp <= 0)
        {
            Debug.LogWarning("ゲームオーバー！");
            Invoke(nameof(BattleSetup), 3.0f);
        }
    }

    private void ApplyDamageToEnemy(int amount)
    {
        _enemyHp = Mathf.Max(0, _enemyHp - amount);
        if (_enemyHp <= 0)
        {
            Debug.LogWarning("勝利！");

            // ART_009 / ART_012: 勝利時の利息＆報酬精算
            if (MoneyManager.Instance != null && _artifactManager != null)
            {
                bool hasBonus = _artifactManager.HasBonusInterestArtifact();
                MoneyManager.Instance.CalculateWinReward(hasBonus);
            }

            Invoke(nameof(BattleSetup), 3.0f);
        }
    }

    private void ChangeTurn(BattleTurn nextTurn)
    {
        if (_cylinderController.RemainingBullets <= 0)
        {
            _cylinderController.SetCylinder();
            _currentTurn = BattleTurn.Player;
            NotifyTurnStart();
            return;
        }

        _currentTurn = nextTurn;
        NotifyTurnStart();

        if (_currentTurn == BattleTurn.Enemy) StartCoroutine(EnemyTurnRoutine());
    }

    private void NotifyTurnStart()
    {
        if (_currentTurn == BattleTurn.Player && _artifactManager != null)
        {
            // ART_003: 次弾予知
            if (_artifactManager.CheckPredictNextBullet())
            {
                bool isNextReal = _cylinderController.PeekNextBullet();
                Debug.Log($"[予知] 次の弾薬種: {(isNextReal ? "実弾" : "空砲")}");
            }
        }
    }

    private IEnumerator EnemyTurnRoutine()
    {
        yield return new WaitForSeconds(1.0f);
        if (_battleAI == null || _cylinderController == null) yield break;

        AIAction action = _battleAI.DecideAction(
            _cylinderController.RemainingBullets,
            _cylinderController.RemainingRealBullets,
            _enemyHp,
            _playerHp,
            _maxHp
        );

        if (action == AIAction.ShootSelf) OnEnemyShootSelf();
        else OnEnemyShootOpponent();
    }
}