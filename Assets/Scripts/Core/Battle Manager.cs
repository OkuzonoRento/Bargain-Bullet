using UnityEngine;
using System.Collections;

public enum BattleTurn
{
    Player,
    Enemy
}

public class BattleManager : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private CylinderController _cylinderController;
    [SerializeField] private BattleAI _battleAI;

    [Header("ステータス")]
    [SerializeField, Min(2)] private int _playerHp = 2;
    [SerializeField, Min(2)] private int _enemyHp = 2;
    private int _randomMinHp = 2;
    private int _randomMaxHp = 6;
    private int _maxHp;

    [Header("状態")]
    [SerializeField] private BattleTurn _currentTurn = BattleTurn.Player;

    private void Start()
    {
        BattleSetup();
    }

    /// <summary>
    /// バトルの初期化
    /// </summary>
    public void BattleSetup()
    {
        _maxHp = Random.Range(_randomMinHp, _randomMaxHp);
        _playerHp = _maxHp;
        _enemyHp = _maxHp;

        if (_cylinderController != null)
        {
            _cylinderController.SetCylinder();
        }
        _currentTurn = BattleTurn.Player;
    }

    #region Player Actions

    public void OnPlayerShootSelf()
    {
        if (_currentTurn != BattleTurn.Player) return;
        ExecuteShoot(targetIsEnemy: false, nextTurnIfHit: BattleTurn.Enemy, nextTurnIfMiss: BattleTurn.Player);
    }

    public void OnPlayerShootOpponent()
    {
        if (_currentTurn != BattleTurn.Player) return;
        ExecuteShoot(targetIsEnemy: true, nextTurnIfHit: BattleTurn.Enemy, nextTurnIfMiss: BattleTurn.Enemy);
    }

    #endregion

    #region Enemy Actions

    public void OnEnemyShootSelf()
    {
        if (_currentTurn != BattleTurn.Enemy) return;
        ExecuteShoot(targetIsEnemy: true, nextTurnIfHit: BattleTurn.Player, nextTurnIfMiss: BattleTurn.Enemy);
    }

    public void OnEnemyShootOpponent()
    {
        if (_currentTurn != BattleTurn.Enemy) return;
        ExecuteShoot(targetIsEnemy: false, nextTurnIfHit: BattleTurn.Player, nextTurnIfMiss: BattleTurn.Player);
    }

    #endregion

    /// <summary>
    /// 射撃処理の共通化メソッド
    /// </summary>
    private void ExecuteShoot(bool targetIsEnemy, BattleTurn nextTurnIfHit, BattleTurn nextTurnIfMiss)
    {
        bool isReal = _cylinderController.GetShot();

        if (isReal)
        {
            if (targetIsEnemy)
            {
                _enemyHp--;
                Debug.Log($"実弾！ 敵の残りHP: {_enemyHp}");
                if (_enemyHp <= 0)
                {
                    Debug.LogWarning("勝利！ 3秒後に再スタートします");
                    Invoke(nameof(BattleSetup), 3.0f);
                    return;
                }
            }
            else
            {
                _playerHp--;
                Debug.Log($"実弾！ 自分の残りHP: {_playerHp}");
                if (_playerHp <= 0)
                {
                    Debug.LogWarning("ゲームオーバー！ 3秒後に再スタートします");
                    Invoke(nameof(BattleSetup), 3.0f);
                    return;
                }
            }

            ChangeTurn(nextTurnIfHit);
        }
        else
        {
            Debug.Log("空砲！");
            ChangeTurn(nextTurnIfMiss);
        }
    }

    /// <summary>
    /// ターン切り替え処理
    /// </summary>
    private void ChangeTurn(BattleTurn nextTurn)
    {
        // 弾切れ時はリロードして強制的に Player ターンへ
        if (CheckBulletAndReload())
        {
            _currentTurn = BattleTurn.Player;
            return;
        }

        _currentTurn = nextTurn;
        Debug.Log($"ターン変更: {_currentTurn}");

        if (_currentTurn == BattleTurn.Enemy)
        {
            StartCoroutine(EnemyTurnRoutine());
        }
    }

    /// <summary>
    /// 敵ターンの思考・実行コルーチン
    /// </summary>
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

        if (action == AIAction.ShootSelf)
        {
            OnEnemyShootSelf();
        }
        else
        {
            OnEnemyShootOpponent();
        }
    }

    /// <summary>
    /// 残弾数の安全チェックと自動リロード
    /// </summary>
    /// <returns>リロードが発生した場合は true</returns>
    private bool CheckBulletAndReload()
    {
        if (_cylinderController.RemainingBullets <= 0)
        {
            Debug.Log($"弾切れのため再装填。ターン変更");
            _cylinderController.SetCylinder();
            return true;
        }
        return false;
    }
}