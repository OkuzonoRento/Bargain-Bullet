using System;
using System.Collections.Generic;
using UnityEngine;
using BargainBullet.Economy;
using BargainBullet.UI;

namespace BargainBullet.Artifacts
{
    public class ArtifactManager : MonoBehaviour
    {
        [SerializeField] private List<ArtifactData> initialArtifacts;
        [SerializeField] private Transform[] tableSlots;

        public List<ArtifactInstance> EquippedArtifacts { get; private set; } = new List<ArtifactInstance>();
        private readonly List<int> _delayedDamageQueue = new List<int>();

        public event Action<int> OnBurnOutDamageToEnemy; // ART_015用イベント通知[cite: 8]

        private void Awake()
        {
            if (initialArtifacts == null) return;
            for (int i = 0; i < initialArtifacts.Count; i++)
            {
                Transform slot = (tableSlots != null && i < tableSlots.Length) ? tableSlots[i] : null;
                EquipArtifact(initialArtifacts[i], slot);
            }
        }

        public void EquipArtifact(ArtifactData data, Transform slotTransform = null)
        {
            var instance = new ArtifactInstance(data);
            instance.OnBurnedOut += HandleItemBurnOut; //[cite: 8]
            EquippedArtifacts.Add(instance);

            if (data.artifactPrefab != null && slotTransform != null)
            {
                GameObject obj = Instantiate(data.artifactPrefab, slotTransform);
                var visual = obj.GetComponent<ArtifactVisual>();
                if (visual != null)
                {
                    instance.OnHeatChanged += (item, ratio) => visual.UpdateHeatVisual(ratio); //[cite: 8]
                }
            }
        }

        // =========================================================
        // ★ 汎用化・最適化済みエフェクト実行基盤 (演出連動)
        // =========================================================

        private bool TryProcArtifact(TriggerCondition trigger, EffectType effect, Action<ArtifactInstance> onSuccess)
        {
            for (int i = EquippedArtifacts.Count - 1; i >= 0; i--)
            {
                var item = EquippedArtifacts[i];
                if (item.IsBurnedOut || item.MasterData.triggerCondition != trigger || item.MasterData.effectType != effect) continue;

                if (UnityEngine.Random.value <= item.MasterData.probability)
                {
                    Debug.Log($"[CutIn Queue] {item.MasterData.artifactName} 発動！");

                    // ★ キューへ追加してコンボ化
                    if (ArtifactCutInUI.Instance != null)
                    {
                        ArtifactCutInUI.Instance.EnqueueCutIn(item.MasterData.artifactName, item.MasterData.artifactPrefab);
                    }

                    onSuccess?.Invoke(item);

                    if (item.MasterData.artifactType == ArtifactType.Disposable)
                    {
                        item.AddHeat(1);
                    }
                    return true;
                }
            }
            return false;
        }

        private void HandleItemBurnOut(ArtifactInstance item)
        {
            // ART_014: 熱限界時にリセット＆身代わり[cite: 8]
            var coolSpray = EquippedArtifacts.Find(x => !x.IsBurnedOut && x.MasterData.effectType == EffectType.ResetHeatOfItem);
            if (coolSpray != null)
            {
                item.ResetHeat();
                coolSpray.AddHeat(1);
                return;
            }

            // ART_015: 焼失時に相手へ1ダメージ[cite: 8]
            TryProcArtifact(TriggerCondition.OnItemBurnOut, EffectType.DamageOnBurnOut, _ => {
                OnBurnOutDamageToEnemy?.Invoke(1); //[cite: 8]
            });

            EquippedArtifacts.Remove(item); //[cite: 8]
        }

        private void CoolAdjacentItems(ArtifactInstance origin, int amount)
        {
            int index = EquippedArtifacts.IndexOf(origin);
            if (index > 0) EquippedArtifacts[index - 1].CoolHeat(amount); //[cite: 8]
            if (index < EquippedArtifacts.Count - 1) EquippedArtifacts[index + 1].CoolHeat(amount); //[cite: 8]
        }

        // =========================================================
        // ★ 各種効果判定 API
        // =========================================================

        // ART_001 & ART_005: 被弾時判定 (遅延 & 反射)[cite: 8]
        public (int finalDamage, int reflectDamage) ProcessIncomingDamage(int rawDamage)
        {
            int immediateDamage = rawDamage;
            int reflectDamage = 0;

            // ART_001: ダメージ遅延[cite: 8]
            TryProcArtifact(TriggerCondition.OnTakeDamage, EffectType.DelayDamage, _ => {
                for (int d = 0; d < rawDamage; d++) _delayedDamageQueue.Add(3); //[cite: 8]
                immediateDamage = 0;
            });

            // ART_005: 反射ダメージ[cite: 8]
            TryProcArtifact(TriggerCondition.OnTakeDamage, EffectType.ReflectDamage, _ => {
                reflectDamage = rawDamage;
            });

            return (immediateDamage, reflectDamage);
        }

        // ART_001: 射撃通過カウントダウン[cite: 8]
        public int OnShotExecuted()
        {
            int triggeredDamage = 0;
            for (int i = _delayedDamageQueue.Count - 1; i >= 0; i--) //[cite: 8]
            {
                _delayedDamageQueue[i]--;
                if (_delayedDamageQueue[i] <= 0)
                {
                    triggeredDamage++;
                    _delayedDamageQueue.RemoveAt(i); //[cite: 8]
                }
            }
            return triggeredDamage;
        }

        // ART_002: 隣接冷却判定[cite: 8]
        public void CheckCoolAdjacentOnSelfBlank()
        {
            TryProcArtifact(TriggerCondition.OnShootSelfBlank, EffectType.CoolAdjacent, item => {
                CoolAdjacentItems(item, (int)item.MasterData.effectValue); //[cite: 8]
            });
        }

        // ART_003: 予知 (スコープ)[cite: 8]
        public bool CheckPredictNextBullet()
        {
            bool predicted = false;
            TryProcArtifact(TriggerCondition.OnTurnStart, EffectType.PredictNext, _ => {
                predicted = true;
            });
            return predicted;
        }

        // ART_004: 自分実弾時の不発化[cite: 8]
        public bool CheckJamSelfReal()
        {
            return TryProcArtifact(TriggerCondition.OnShootSelfReal, EffectType.JamSelfReal, null); //[cite: 8]
        }

        // ART_006: 自分空砲時の回復[cite: 8]
        public int CheckHealOnSelfBlank()
        {
            int heal = 0;
            TryProcArtifact(TriggerCondition.OnShootSelfBlank, EffectType.AddHp, item => {
                heal = (int)item.MasterData.effectValue; //[cite: 8]
            });
            return heal;
        }

        // ART_007: 与ダメージ上昇[cite: 8]
        public int ProcessOutgoingDamage(int baseDamage)
        {
            int bonus = 0;
            TryProcArtifact(TriggerCondition.OnAttackReal, EffectType.DamageBoost, item => {
                bonus += (int)item.MasterData.effectValue; //[cite: 8]
            });
            return baseDamage + bonus;
        }

        // ART_008 & ART_012: 致死耐え[cite: 8]
        public bool CheckFatalProtection()
        {
            bool survived = TryProcArtifact(TriggerCondition.OnFatalDamage, EffectType.SurviveHP1, null); //[cite: 8]
            if (!survived)
            {
                survived = TryProcArtifact(TriggerCondition.OnFatalDamage, EffectType.SurviveAndSeizeInterest, _ => {
                    // ART_012: 保険発動時はマネーマネージャーへ通知
                    if (MoneyManager.Instance != null)
                    {
                        MoneyManager.Instance.SetSeizedInterestFlag();
                    }
                });
            }
            return survived;
        }

        // ART_010: 3連続空砲での実弾化通知[cite: 8]
        public void CheckThreeBlanks()
        {
            TryProcArtifact(TriggerCondition.OnThreeBlanks, EffectType.ForceRealBullet, null); //[cite: 8]
        }

        // ART_011: ターン強奪[cite: 8]
        public bool CheckTurnSteal()
        {
            return TryProcArtifact(TriggerCondition.OnOpponentShootSelfBlank, EffectType.StealTurn, null); //[cite: 8]
        }

        // ART_013: 自分実弾被弾時の保険金精算
        public void CheckInsuranceOnSelfShootReal(int damage)
        {
            TryProcArtifact(TriggerCondition.OnShootSelfReal, EffectType.InsuranceByDamage, item => {
                float money = damage * item.MasterData.effectValue;
                if (MoneyManager.Instance != null)
                {
                    MoneyManager.Instance.AddInsuranceMoney(money);
                }
            });
        }

        // ART_009: ボーナス金利所持チェック
        public bool HasBonusInterestArtifact()
        {
            return EquippedArtifacts.Exists(x => !x.IsBurnedOut && x.MasterData.effectType == EffectType.BonusInterest);
        }
    }
}