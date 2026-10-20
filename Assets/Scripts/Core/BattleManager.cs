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
    [SerializeField] private EnemyController _enemyController;

    [Header("アーティファクト管理")]
    [SerializeField] private ArtifactManager _artifactManager;

    [Header("ランタンUI参照")]
    [SerializeField] private LanternHealthController _playerLanternController;
    [SerializeField] private LanternHealthController _enemyLanternController;

    [Header("カメラ・演出制御")]
    [SerializeField] private BattleCameraController _battleCameraController;

    [Header("サウンド設定")]
    [SerializeField] private AudioClip _realShootSE;   // 実弾発砲時のSE
    [SerializeField] private AudioClip _blankShootSE;  // 空砲発砲時のSE
    private AudioSource _audioSource;

    [Header("ステータス")]
    [SerializeField, Min(2)] private int _playerHp = 2;
    [SerializeField, Min(2)] private int _enemyHp = 2;
    private int _maxHp;
    private int _consecutiveBlanks = 0;

    [Header("状態")]
    [SerializeField] private BattleTurn _currentTurn = BattleTurn.Player;
    private bool _isInputLocked = true;

    // アニメーション終了フラグ
    private bool _isAnimFinished = false;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (_artifactManager != null)
        {
            _artifactManager.OnBurnOutDamageToEnemy += amount => ApplyDamageToEnemy(amount);
        }

        if (_enemyController != null)
        {
            _enemyController.OnActionAnimationFinished += () => _isAnimFinished = true;
            _enemyController.OnReloadAnimationFinished += () => _isAnimFinished = true;
        }

        BattleSetup();
    }

    public void BattleSetup()
    {
        _isInputLocked = true;
        _maxHp = Random.Range(2, 6);
        _playerHp = _maxHp;
        _enemyHp = _maxHp;
        _consecutiveBlanks = 0;

        if (_battleCameraController != null) _battleCameraController.ResetCamera();

        UpdateLanterns();
        _currentTurn = BattleTurn.Player;

        StartCoroutine(InitialSetupRoutine());
    }

    private IEnumerator InitialSetupRoutine()
    {
        _isInputLocked = true;

        if (_battleCameraController != null)
        {
            while (_battleCameraController.IsMoving) yield return null;
        }

        _isAnimFinished = false;
        if (_enemyController != null)
        {
            _enemyController.PlayReloadAnimation();
        }

        if (_cylinderController != null)
        {
            _cylinderController.SetCylinder();
        }

        float timer = 0f;
        while (!_isAnimFinished && timer < 2.5f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(NotifyTurnStartRoutine());

        _isInputLocked = false;
    }

    #region Action Endpoints (Player & Enemy Input triggers)

    public void OnPlayerShootSelf()
    {
        if (_isInputLocked || (_battleCameraController != null && _battleCameraController.IsMoving) || _currentTurn != BattleTurn.Player) return;
        _isInputLocked = true;

        if (_battleCameraController != null)
        {
            _battleCameraController.SwitchCameraForTarget(targetIsEnemy: false, isShootingSelf: true);
            _battleCameraController.PlayRecoil(isHeavyHit: false);
        }

        StartCoroutine(ExecuteShootRoutine(targetIsEnemy: false, nextTurnIfHit: BattleTurn.Enemy, nextTurnIfMiss: BattleTurn.Player));
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

        StartCoroutine(ExecuteShootRoutine(targetIsEnemy: true, nextTurnIfHit: BattleTurn.Enemy, nextTurnIfMiss: BattleTurn.Enemy));
    }

    public void OnEnemyShootSelf()
    {
        if (_currentTurn == BattleTurn.Enemy)
        {
            _isAnimFinished = false;
            if (_enemyController != null)
            {
                _enemyController.PlayMyShotAnimation();
            }
            StartCoroutine(ExecuteShootTargetForEnemyRoutine(isShootingEnemySelf: true));
        }
    }

    public void OnEnemyShootOpponent()
    {
        if (_currentTurn == BattleTurn.Enemy)
        {
            _isAnimFinished = false;
            if (_enemyController != null)
            {
                _enemyController.PlayShotAnimation();
            }
            StartCoroutine(ExecuteShootTargetForEnemyRoutine(isShootingEnemySelf: false));
        }
    }

    #endregion

    private IEnumerator ExecuteShootRoutine(bool targetIsEnemy, BattleTurn nextTurnIfHit, BattleTurn nextTurnIfMiss)
    {
        yield return new WaitForSeconds(0.5f);

        bool isEnemyTurn = false;

        if (_artifactManager != null)
        {
            int dueDamage = _artifactManager.OnShotExecuted(isEnemyTurn);
            if (dueDamage > 0)
            {
                if (targetIsEnemy)
                {
                    PlayShootSE(true);
                    _isAnimFinished = false;
                    if (_enemyController != null) _enemyController.PlayHitAnimation();
                    ApplyDamageToEnemy(dueDamage);
                }
                else
                {
                    PlayShootSE(true);
                    ApplyDirectDamageToPlayer(dueDamage);
                }
                if (_playerHp <= 0 || _enemyHp <= 0) yield break;
            }

            if (!targetIsEnemy && _artifactManager.CheckJamSelfReal(isEnemy: false))
            {
                Debug.Log("[Artifact] ジャム発動！");
                ChangeTurn(BattleTurn.Player);
                yield break;
            }
        }

        bool isReal = _cylinderController.GetShot();

        yield return new WaitForSeconds(0.3f);

        PlayShootSE(isReal);

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
                _isAnimFinished = false;
                if (_enemyController != null)
                {
                    _enemyController.PlayHitAnimation();
                }

                float timer = 0f;
                while (!_isAnimFinished && timer < 2.0f)
                {
                    timer += Time.deltaTime;
                    yield return null;
                }

                ApplyDamageToEnemy(damage);
                if (_enemyHp > 0) ChangeTurn(nextTurnIfHit);
            }
            else
            {
                if (_battleCameraController != null)
                {
                    yield return StartCoroutine(_battleCameraController.PlayBlackoutWakeUpRoutine());
                }

                ApplyDirectDamageToPlayer(damage);
                if (_playerHp > 0) ChangeTurn(nextTurnIfHit);
            }
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
                    _playerHp = Mathf.Min(_maxHp, _playerHp + heal);
                }
            }
            ChangeTurn(nextTurnIfMiss);
        }
    }

    private IEnumerator ExecuteShootTargetForEnemyRoutine(bool isShootingEnemySelf)
    {
        float timer = 0f;
        while (!_isAnimFinished && timer < 2.5f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        bool isEnemyTurn = true;

        if (_artifactManager != null)
        {
            int dueDamage = _artifactManager.OnShotExecuted(isEnemyTurn);
            if (dueDamage > 0)
            {
                if (isShootingEnemySelf)
                {
                    PlayShootSE(true);
                    _isAnimFinished = false;
                    if (_enemyController != null) _enemyController.PlayHitAnimation();
                    ApplyDamageToEnemy(dueDamage);
                }
                else
                {
                    PlayShootSE(true);
                    if (_battleCameraController != null)
                    {
                        yield return StartCoroutine(_battleCameraController.PlayBlackoutWakeUpRoutine());
                    }
                    ApplyDirectDamageToPlayer(dueDamage);
                }
                if (_playerHp <= 0 || _enemyHp <= 0) yield break;
            }

            if (isShootingEnemySelf && _artifactManager.CheckJamSelfReal(isEnemy: true))
            {
                Debug.Log("[Artifact] 敵のジャム発動！");
                ChangeTurn(BattleTurn.Enemy);
                yield break;
            }
        }

        bool isReal = _cylinderController.GetShot();

        yield return new WaitForSeconds(0.3f);

        PlayShootSE(isReal);

        if (isReal)
        {
            _consecutiveBlanks = 0;
            int damage = 1;

            if (_artifactManager != null)
            {
                damage = _artifactManager.ProcessOutgoingDamage(damage, isEnemyTurn);
            }

            if (isShootingEnemySelf)
            {
                _isAnimFinished = false;
                if (_enemyController != null)
                {
                    _enemyController.PlayHitAnimation();
                }

                timer = 0f;
                while (!_isAnimFinished && timer < 2.0f)
                {
                    timer += Time.deltaTime;
                    yield return null;
                }

                ApplyDamageToEnemy(damage);
                if (_enemyHp > 0 && _playerHp > 0) ChangeTurn(BattleTurn.Player);
            }
            else
            {
                if (_battleCameraController != null)
                {
                    yield return StartCoroutine(_battleCameraController.PlayBlackoutWakeUpRoutine());
                }

                ApplyDirectDamageToPlayer(damage);
                if (_enemyHp > 0 && _playerHp > 0) ChangeTurn(BattleTurn.Player);
            }
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
                    _enemyHp = Mathf.Min(_maxHp, _enemyHp + heal);
                }
            }

            BattleTurn nextTurn = isShootingEnemySelf ? BattleTurn.Enemy : BattleTurn.Player;
            ChangeTurn(nextTurn);
        }
    }

    private void PlayShootSE(bool isReal)
    {
        if (_audioSource == null) return;

        if (isReal && _realShootSE != null)
        {
            _audioSource.PlayOneShot(_realShootSE);
        }
        else if (!isReal && _blankShootSE != null)
        {
            _audioSource.PlayOneShot(_blankShootSE);
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

            if (reflect > 0) ApplyDamageToEnemy(reflect);
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
            var damageReflectResult = _artifactManager.ProcessIncomingDamage(baseAmount, isTargetEnemy: true);
            finalDamage = damageReflectResult.Item1;
            int reflect = damageReflectResult.Item2;

            if (reflect > 0) ApplyDirectDamageToPlayer(reflect);
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
        _isInputLocked = true;

        if (_cylinderController.RemainingBullets <= 0)
        {
            if (_battleCameraController != null)
            {
                _battleCameraController.ResetCamera();
            }

            StartCoroutine(ReloadAndResetRoutine(BattleTurn.Player));
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

    private IEnumerator ReloadAndResetRoutine(BattleTurn nextTurn)
    {
        if (_battleCameraController != null)
        {
            while (_battleCameraController.IsMoving) yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        _isAnimFinished = false;
        if (_enemyController != null)
        {
            _enemyController.PlayReloadAnimation();
        }

        if (_cylinderController != null)
        {
            _cylinderController.SetCylinder();
        }

        float timer = 0f;
        while (!_isAnimFinished && timer < 2.5f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);

        _currentTurn = nextTurn;

        yield return StartCoroutine(NotifyTurnStartRoutine());
        _isInputLocked = false;
    }

    private IEnumerator WaitCameraAndProcessArtifactsRoutine(bool nextTurnIsPlayer)
    {
        _isInputLocked = true;

        if (_battleCameraController != null)
        {
            while (_battleCameraController.IsMoving)
            {
                yield return null;
            }
        }

        yield return new WaitForSeconds(0.2f);

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
                    yield return new WaitForSeconds(1.5f);
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
        _isInputLocked = true;

        yield return new WaitForSeconds(0.8f);

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