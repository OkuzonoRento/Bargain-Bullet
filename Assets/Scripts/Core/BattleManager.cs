using BargainBullet.Artifacts;
using BargainBullet.Core;
using BargainBullet.Economy;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleTurn { Player, Enemy }

public class BattleManager : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private CylinderController _cylinderController;
    [SerializeField] private BattleAI _battleAI;

    [Header("アーティファクト管理")]
    [SerializeField] private ArtifactManager _artifactManager;

    [Header("ランタンUI参照")]
    [SerializeField] private LanternHealthController _playerLanternController;
    [SerializeField] private LanternHealthController _enemyLanternController;

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

        UpdateLanterns();

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
        bool isEnemyTurn = (_currentTurn == BattleTurn.Enemy);

        if (_artifactManager != null)
        {
            int dueDamage = _artifactManager.OnShotExecuted(isEnemyTurn);
            if (dueDamage > 0)
            {
                if (isEnemyTurn) ApplyDamageToEnemy(dueDamage);
                else ApplyDirectDamageToPlayer(dueDamage);
                if (_playerHp <= 0 || _enemyHp <= 0) return;
            }

            if (!isEnemyTurn && !targetIsEnemy)
            {
                if (_artifactManager.CheckJamSelfReal(isEnemy: false))
                {
                    Debug.Log("[Artifact] ジャム発動！");
                    ChangeTurn(BattleTurn.Player);
                    return;
                }
            }
            else if (isEnemyTurn && targetIsEnemy)
            {
                if (_artifactManager.CheckJamSelfReal(isEnemy: true))
                {
                    Debug.Log("[Artifact] 敵のジャム発動！");
                    ChangeTurn(BattleTurn.Enemy);
                    return;
                }
            }
        }

        // 実際の弾の発射・判定
        bool isReal = _cylinderController.GetShot();

        if (isReal)
        {
            _consecutiveBlanks = 0;
            int damage = 1;

            if (_artifactManager != null)
            {
                damage = _artifactManager.ProcessOutgoingDamage(damage, isEnemyTurn);
            }

            if (targetIsEnemy)
            {
                if (isEnemyTurn) ApplyDirectDamageToPlayer(damage);
                else ApplyDamageToEnemy(damage);
            }
            else
            {
                if (isEnemyTurn) ApplyDamageToEnemy(damage);
                else ApplyDirectDamageToPlayer(damage);
            }

            if (_playerHp > 0 && _enemyHp > 0) ChangeTurn(nextTurnIfHit);
        }
        else
        {
            _consecutiveBlanks++;
            if (_artifactManager != null)
            {
                _artifactManager.CheckCoolAdjacentOnSelfBlank(isEnemyTurn);
                int heal = _artifactManager.CheckHealOnSelfBlank(isEnemyTurn);
                if (heal > 0)
                {
                    if (isEnemyTurn) _enemyHp = Mathf.Min(_maxHp, _enemyHp + heal);
                    else _playerHp = Mathf.Min(_maxHp, _playerHp + heal);
                }
            }
            ChangeTurn(nextTurnIfMiss);
        }

        // =========================================================
        // ★ 発砲処理がすべて終わった後に、オブジェクト消去と「壊れたか」表示を実行
        // =========================================================
        if (!isEnemyTurn && _artifactManager != null)
        {
            List<ArtifactInstance> burnedItems = _artifactManager.ExtractBurnedOutItems(isEnemy: false);
            if (burnedItems.Count > 0)
            {
                if (_cylinderController != null)
                {
                    _cylinderController.ShowTextInfo("壊れたか");
                }
            }
        }
    }

    private void ApplyDirectDamageToPlayer(int baseAmount)
    {
        int finalDamage = baseAmount;

        if (_artifactManager != null)
        {
            var (damage, reflect) = _artifactManager.ProcessIncomingDamage(baseAmount, isTargetEnemy: false);
            finalDamage = damage;

            if (reflect > 0)
            {
                ApplyDamageToEnemy(reflect);
            }
        }

        if (finalDamage > 0)
        {
            bool isFatal = (_playerHp - finalDamage) <= 0;

            if (isFatal && _artifactManager != null && _artifactManager.CheckFatalProtection(isEnemy: false))
            {
                _playerHp = 1;
            }
            else
            {
                _playerHp = Mathf.Max(0, _playerHp - finalDamage);
            }

            if (_playerLanternController != null) _playerLanternController.UpdateLanternGlow(_playerHp);

            if (_playerHp <= 0)
            {
                Debug.LogWarning("ゲームオーバー！");
                Invoke(nameof(BattleSetup), 3.0f);
            }
        }
    }

    private void ApplyDamageToEnemy(int baseAmount)
    {
        int finalDamage = baseAmount;

        if (_artifactManager != null)
        {
            var (damage, reflect) = _artifactManager.ProcessIncomingDamage(baseAmount, isTargetEnemy: true);
            finalDamage = damage;

            if (reflect > 0)
            {
                ApplyDirectDamageToPlayer(reflect);
            }
        }

        if (finalDamage > 0)
        {
            bool isFatal = (_enemyHp - finalDamage) <= 0;

            if (isFatal && _artifactManager != null && _artifactManager.CheckFatalProtection(isEnemy: true))
            {
                _enemyHp = 1;
            }
            else
            {
                _enemyHp = Mathf.Max(0, _enemyHp - finalDamage);
            }

            if (_enemyLanternController != null) _enemyLanternController.UpdateLanternGlow(_enemyHp);

            if (_enemyHp <= 0)
            {
                Debug.LogWarning("勝利！");
                if (MoneyManager.Instance != null && _artifactManager != null)
                {
                    bool hasBonus = _artifactManager.HasBonusInterestArtifact();
                    MoneyManager.Instance.CalculateWinReward(hasBonus);
                }
                Invoke(nameof(BattleSetup), 3.0f);
            }
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
            // ART_003: 次弾予知（「次は実弾な気がする」「次は空砲な気がする」に変更）
            if (_artifactManager.CheckPredictNextBullet())
            {
                bool isNextReal = _cylinderController.PeekNextBullet();
                string predictMsg = isNextReal ? "次は実弾な気がする" : "次は空砲な気がする";

                if (_cylinderController != null)
                {
                    _cylinderController.ShowTextInfo(predictMsg);
                }
            }
        }
    }

    private void UpdateLanterns()
    {
        if (_playerLanternController != null) _playerLanternController.UpdateLanternGlow(_playerHp);
        if (_enemyLanternController != null) _enemyLanternController.UpdateLanternGlow(_enemyHp);
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
            _maxHp,
            _artifactManager
        );

        if (action == AIAction.ShootSelf) OnEnemyShootSelf();
        else OnEnemyShootOpponent();
    }
}