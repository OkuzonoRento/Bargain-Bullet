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

    [Header("カメラ・演出制御")]
    [SerializeField] private BattleCameraController _battleCameraController;

    [Header("ステータス")]
    [SerializeField, Min(2)] private int _playerHp = 2;
    [SerializeField, Min(2)] private int _enemyHp = 2;
    private int _maxHp;
    private int _consecutiveBlanks = 0;

    [Header("状態")]
    [SerializeField] private BattleTurn _currentTurn = BattleTurn.Player;
    private bool _isInputLocked = true; // 初期状態はロックしておく

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
        if (_battleCameraController != null) _battleCameraController.ResetCamera();

        UpdateLanterns();

        _currentTurn = BattleTurn.Player;

        // セットアップ時もカメラの初期化とターン開始テキストの終了を待ってから入力を解禁
        StartCoroutine(WaitInitialSetupAndUnlockInput());
    }

    private IEnumerator WaitInitialSetupAndUnlockInput()
    {
        _isInputLocked = true;

        if (_battleCameraController != null)
        {
            while (_battleCameraController.IsMoving) yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        // 修正：yield return に変更
        yield return StartCoroutine(NotifyTurnStartRoutine());

        _isInputLocked = false;
    }

    #region Action Endpoints

    public void OnPlayerShootSelf()
    {
        if (_isInputLocked || (_battleCameraController != null && _battleCameraController.IsMoving) || _currentTurn != BattleTurn.Player) return;
        _isInputLocked = true;

        if (_battleCameraController != null)
        {
            _battleCameraController.SwitchCameraForTarget(targetIsEnemy: false, isShootingSelf: true);
            _battleCameraController.PlayRecoil(isHeavyHit: false);
        }

        ExecuteShoot(targetIsEnemy: false, nextTurnIfHit: BattleTurn.Enemy, nextTurnIfMiss: BattleTurn.Player);
    }

    public void OnPlayerShootOpponent()
    {
        if (_isInputLocked || (_battleCameraController != null && _battleCameraController.IsMoving) || _currentTurn != BattleTurn.Player) return;
        _isInputLocked = true;

        if (_battleCameraController != null)
        {
            _battleCameraController.SwitchCameraForTarget(targetIsEnemy: true, isShootingSelf: false);
            _battleCameraController.PlayRecoil(isHeavyHit: false);
        }

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
    }

    private void ApplyDirectDamageToPlayer(int baseAmount)
    {
        int finalDamage = baseAmount;

        if (_artifactManager != null)
        {
            var damageReflectResult = _artifactManager.ProcessIncomingDamage(baseAmount, isTargetEnemy: false);
            finalDamage = damageReflectResult.Item1;
            int reflect = damageReflectResult.Item2;

            if (reflect > 0)
            {
                ApplyDamageToEnemy(reflect);
            }
        }

        if (finalDamage > 0)
        {
            if (_battleCameraController != null && !(_currentTurn == BattleTurn.Player && _cylinderController.RemainingBullets <= 0))
            {
                StartCoroutine(_battleCameraController.PlayBlackoutWakeUpRoutine());
            }

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
            var damageReflectResult = _artifactManager.ProcessIncomingDamage(baseAmount, isTargetEnemy: true);
            finalDamage = damageReflectResult.Item1;
            int reflect = damageReflectResult.Item2;

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
            if (_battleCameraController != null)
            {
                _battleCameraController.ResetCamera();
                StartCoroutine(WaitCameraAndProcessArtifactsRoutine(nextTurnIsPlayer: true));
            }
            else
            {
                ProcessArtifactBurnoutAndText();
                _isInputLocked = false;
            }
            _currentTurn = BattleTurn.Player;
            return;
        }

        _currentTurn = nextTurn;

        if (_battleCameraController != null)
        {
            if (_currentTurn == BattleTurn.Enemy)
            {
                _battleCameraController.SwitchCameraForTarget(targetIsEnemy: true, isShootingSelf: false);
                StartCoroutine(WaitCameraAndProcessArtifactsRoutine(nextTurnIsPlayer: false));
            }
            else
            {
                _battleCameraController.ResetCamera();
                StartCoroutine(WaitCameraAndProcessArtifactsRoutine(nextTurnIsPlayer: true));
            }
        }
    }

    private IEnumerator WaitCameraAndProcessArtifactsRoutine(bool nextTurnIsPlayer)
    {
        if (_battleCameraController != null)
        {
            while (_battleCameraController.IsMoving)
            {
                yield return null;
            }
        }

        yield return new WaitForSeconds(0.15f);

        if (_artifactManager != null && !(_currentTurn == BattleTurn.Enemy && !nextTurnIsPlayer))
        {
            bool hasBurnedOut = _artifactManager.ProcessAndExtractBurnedOutItems(isEnemy: false, _cylinderController);

            if (hasBurnedOut)
            {
                yield return new WaitForSeconds(1.2f);
            }
        }

        if (!nextTurnIsPlayer)
        {
            StartCoroutine(EnemyTurnRoutine());
        }
        else
        {
            // 修正：yield return に変更
            yield return StartCoroutine(NotifyTurnStartRoutine());
            _isInputLocked = false;
        }
    }

    private IEnumerator NotifyTurnStartRoutine()
    {
        if (_currentTurn == BattleTurn.Player && _artifactManager != null)
        {
            if (_artifactManager.CheckPredictNextBullet())
            {
                bool isNextReal = _cylinderController.PeekNextBullet();
                string predictMsg = isNextReal ? "次は実弾な気がする" : "次は空砲な気がする";

                if (_cylinderController != null)
                {
                    _cylinderController.ShowTextInfo(predictMsg);
                    yield return new WaitForSeconds(1.2f);
                }
            }
        }
    }

    private void ProcessArtifactBurnoutAndText()
    {
        if (_artifactManager != null)
        {
            _artifactManager.ProcessAndExtractBurnedOutItems(isEnemy: false, _cylinderController);
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