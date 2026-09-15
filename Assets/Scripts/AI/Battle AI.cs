using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 敵AIの性格（思考タイプ）
/// </summary>
public enum AIType
{
    Balanced,
    Aggressive,
    Cautious,
    Psychopath
}

/// <summary>
/// AIが選択する行動結果
/// </summary>
public enum AIAction
{
    ShootSelf,
    ShootOpponent
}

public class BattleAI : MonoBehaviour
{
    /// <summary>
    /// 性格ごとの内部パラメータ設定構造体
    /// </summary>
    private struct AIParameter
    {
        public float RateThreshold; // 実弾率の判定基準
        public float DangerRate;    // 瀕死時の危険度基準
        public float MadSelfChance; // 異常者：高実弾率でもあえて自分を撃つ確率
        public float MadGamble;     // 異常者：自分が瀕死でも自傷するギャンブル確率
        public float MadRandom;     // 異常者：通常のランダム行動の分岐基準値
    }

    private AIType _type = AIType.Balanced;
    private Dictionary<AIType, AIParameter> _params;

    private void Awake()
    {
        // パラメータ辞書の初期化
        InitParameters();

        // 登場時にランダムで性格を1つ選定
        Array values = Enum.GetValues(typeof(AIType));
        _type = (AIType)values.GetValue(UnityEngine.Random.Range(0, values.Length));

        Debug.Log($"今回の敵の性格: {_type}");
    }

    /// <summary>
    /// 性格ごとの数値パラメータを一括定義・管理
    /// </summary>
    private void InitParameters()
    {
        _params = new Dictionary<AIType, AIParameter>
        {
            [AIType.Balanced] = new AIParameter
            {
                RateThreshold = 0.5f,
                DangerRate = 0.3f
            },
            [AIType.Aggressive] = new AIParameter
            {
                RateThreshold = 0.4f
            },
            [AIType.Cautious] = new AIParameter
            {
                RateThreshold = 0.5f
            },
            [AIType.Psychopath] = new AIParameter
            {
                RateThreshold = 0.7f,
                MadSelfChance = 0.3f,
                MadGamble = 0.5f,
                MadRandom = 0.5f
            }
        };
    }

    /// <summary>
    /// 現在の盤面情報から最適な行動を決定する
    /// </summary>
    /// <param name="total">残り総弾数</param>
    /// <param name="real">残り実弾数</param>
    /// <param name="enemyHp">敵（自分）の現在HP</param>
    /// <param name="playerHp">プレイヤー（相手）の現在HP</param>
    /// <param name="maxHp">最大HP</param>
    public AIAction DecideAction(int total, int real, int enemyHp, int playerHp, int maxHp)
    {
        if (total <= 0) return AIAction.ShootOpponent;

        float rate = (float)real / total;
        bool enemyDanger = enemyHp == 1;
        bool playerDanger = playerHp == 1;

        AIParameter p = _params[_type];

        switch (_type)
        {
            case AIType.Psychopath:

                if (rate >= p.RateThreshold && UnityEngine.Random.value < p.MadSelfChance)
                {
                    return AIAction.ShootSelf;
                }

                if (enemyDanger && UnityEngine.Random.value < p.MadGamble)
                {
                    return AIAction.ShootSelf;
                }

                return (UnityEngine.Random.value < p.MadRandom) ? AIAction.ShootOpponent : AIAction.ShootSelf;


            case AIType.Aggressive:

                if (playerDanger && real > 0)
                {
                    return AIAction.ShootOpponent;
                }

                return (rate >= p.RateThreshold) ? AIAction.ShootOpponent : AIAction.ShootSelf;


            case AIType.Cautious:

                if (enemyDanger)
                {
                    return (rate == 0f) ? AIAction.ShootSelf : AIAction.ShootOpponent;
                }

                return (rate >= p.RateThreshold) ? AIAction.ShootOpponent : AIAction.ShootSelf;


            case AIType.Balanced:
            default:

                if (enemyDanger && rate > p.DangerRate)
                {
                    return AIAction.ShootOpponent;
                }
                return (rate >= p.RateThreshold) ? AIAction.ShootOpponent : AIAction.ShootSelf;
        }
    }
}