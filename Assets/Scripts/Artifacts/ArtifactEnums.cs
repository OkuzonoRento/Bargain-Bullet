namespace BargainBullet.Artifacts
{
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic
    }

    public enum ArtifactType
    {
        Permanent,  // âië±å^ÅiîMÇ™ó≠Ç‹ÇÁÇ»Ç¢Åj
        Disposable  // è¡ñ≈å^Åiî≠ìÆÇ≤Ç∆Ç…îMÇ™ó≠Ç‹ÇËÅAå¿äEÇ≈íPëÃèƒé∏Åj
    }

    public enum TriggerCondition
    {
        OnTakeDamage,
        OnShootSelfBlank,
        OnShootSelfReal,
        OnTurnStart,
        OnAttackReal,
        OnFatalDamage,
        OnBattleWin,
        OnThreeBlanks,
        OnOpponentShootSelfBlank,
        OnHeatMax,
        OnItemBurnOut
    }

    public enum EffectType
    {
        DelayDamage,
        CoolAdjacent,
        PredictNext,
        JamSelfReal,
        ReflectDamage,
        AddHp,
        DamageBoost,
        SurviveHP1,
        BonusInterest,
        ForceRealBullet,
        StealTurn,
        SurviveAndSeizeInterest,
        InsuranceByDamage,
        ResetHeatOfItem,
        DamageOnBurnOut
    }
}