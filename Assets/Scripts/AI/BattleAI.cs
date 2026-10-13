using UnityEngine;
using System;
using System.Collections.Generic;
using BargainBullet.Artifacts;

public enum AIType
{
    Balanced,
    Aggressive,
    Cautious,
    Psychopath
}

public enum AIAction
{
    ShootSelf,
    ShootOpponent
}

public class BattleAI : MonoBehaviour
{
    private struct AIParameter
    {
        public float RateThreshold;
        public float DangerRate;
        public float MadSelfChance;
        public float MadGamble;
        public float MadRandom;
    }

    private AIType _type = AIType.Balanced;
    private Dictionary<AIType, AIParameter> _params;

    private void Awake()
    {
        InitParameters();
        Array values = Enum.GetValues(typeof(AIType));
        _type = (AIType)values.GetValue(UnityEngine.Random.Range(0, values.Length));
        Debug.Log($"今回の敵の性格: {_type}");
    }

    private void InitParameters()
    {
        _params = new Dictionary<AIType, AIParameter>
        {
            [AIType.Balanced] = new AIParameter { RateThreshold = 0.5f, DangerRate = 0.3f },
            [AIType.Aggressive] = new AIParameter { RateThreshold = 0.4f },
            [AIType.Cautious] = new AIParameter { RateThreshold = 0.5f },
            [AIType.Psychopath] = new AIParameter { RateThreshold = 0.7f, MadSelfChance = 0.3f, MadGamble = 0.5f, MadRandom = 0.5f }
        };
    }

    // アーティファクトのマネージャーを引数に受け取る
    public AIAction DecideAction(int total, int real, int enemyHp, int playerHp, int maxHp, ArtifactManager enemyArtifacts)
    {
        if (total <= 0) return AIAction.ShootOpponent;

        float rate = (float)real / total;
        bool enemyDanger = enemyHp == 1;
        bool playerDanger = playerHp == 1;

        // =========================================================
        // ★ アーティファクトの効果によるAIの行動変化ロジック
        // =========================================================
        if (enemyArtifacts != null)
        {
            // 修正: 第1引数に isEnemy = true を指定して敵側の所持判定を行う
            if (enemyArtifacts.HasEffect(true, EffectType.AddHp) && rate < 0.5f)
            {
                return AIAction.ShootSelf;
            }
        }

        AIParameter p = _params[_type];

        switch (_type)
        {
            case AIType.Psychopath:
                if (rate >= p.RateThreshold && UnityEngine.Random.value < p.MadSelfChance) return AIAction.ShootSelf;
                if (enemyDanger && UnityEngine.Random.value < p.MadGamble) return AIAction.ShootSelf;
                return (UnityEngine.Random.value < p.MadRandom) ? AIAction.ShootOpponent : AIAction.ShootSelf;

            case AIType.Aggressive:
                if (playerDanger && real > 0) return AIAction.ShootOpponent;
                return (rate >= p.RateThreshold) ? AIAction.ShootOpponent : AIAction.ShootSelf;

            case AIType.Cautious:
                if (enemyDanger) return (rate == 0f) ? AIAction.ShootSelf : AIAction.ShootOpponent;
                return (rate >= p.RateThreshold) ? AIAction.ShootOpponent : AIAction.ShootSelf;

            case AIType.Balanced:
            default:
                if (enemyDanger && rate > p.DangerRate) return AIAction.ShootOpponent;
                return (rate >= p.RateThreshold) ? AIAction.ShootOpponent : AIAction.ShootSelf;
        }
    }
}