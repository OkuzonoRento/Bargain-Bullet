using System;
using System.Collections.Generic;
using UnityEngine;
using BargainBullet.Economy;
using BargainBullet.UI;

namespace BargainBullet.Artifacts
{
    public class ArtifactManager : MonoBehaviour
    {
        [Header("■ プレイヤー側 設定")]
        [SerializeField] private List<ArtifactData> playerInitialArtifacts;
        [SerializeField] private Transform[] playerTableSlots;

        [Header("■ 敵側 設定")]
        [SerializeField] private List<ArtifactData> enemyInitialArtifacts;
        [SerializeField] private Transform[] enemyTableSlots;

        public List<ArtifactInstance> PlayerEquippedArtifacts { get; private set; } = new List<ArtifactInstance>();
        public List<ArtifactInstance> EnemyEquippedArtifacts { get; private set; } = new List<ArtifactInstance>();

        private readonly List<int> _playerDelayedDamageQueue = new List<int>();
        private readonly List<int> _enemyDelayedDamageQueue = new List<int>();

        public event Action<int> OnBurnOutDamageToEnemy;

        private void Awake()
        {
            if (playerInitialArtifacts != null)
            {
                for (int i = 0; i < playerInitialArtifacts.Count; i++)
                {
                    if (playerInitialArtifacts[i] == null) continue;
                    Transform slot = (playerTableSlots != null && i < playerTableSlots.Length) ? playerTableSlots[i] : null;
                    EquipArtifact(playerInitialArtifacts[i], slot, isEnemy: false);
                }
            }

            if (enemyInitialArtifacts != null)
            {
                for (int i = 0; i < enemyInitialArtifacts.Count; i++)
                {
                    if (enemyInitialArtifacts[i] == null) continue;
                    Transform slot = (enemyTableSlots != null && i < enemyTableSlots.Length) ? enemyTableSlots[i] : null;
                    EquipArtifact(enemyInitialArtifacts[i], slot, isEnemy: true);
                }
            }
        }

        public void EquipArtifact(ArtifactData data, Transform slotTransform, bool isEnemy)
        {
            var instance = new ArtifactInstance(data);
            instance.OnBurnedOut += (item) => HandleItemBurnOut(item, isEnemy);

            if (isEnemy)
            {
                EnemyEquippedArtifacts.Add(instance);
            }
            else
            {
                PlayerEquippedArtifacts.Add(instance);
            }

            if (data.artifactPrefab != null && slotTransform != null)
            {
                GameObject obj = Instantiate(data.artifactPrefab, slotTransform);
                instance.VisualGameObject = obj;

                var visual = obj.GetComponent<ArtifactVisual>();
                if (visual != null)
                {
                    instance.OnHeatChanged += (item, ratio) => visual.UpdateHeatVisual(ratio);
                }
            }
        }

        private bool TryProcArtifact(List<ArtifactInstance> targetList, TriggerCondition trigger, EffectType effect, Action<ArtifactInstance> onSuccess, bool isEnemy)
        {
            for (int i = targetList.Count - 1; i >= 0; i--)
            {
                var item = targetList[i];
                if (item.IsBurnedOut || item.MasterData.triggerCondition != trigger || item.MasterData.effectType != effect) continue;

                if (UnityEngine.Random.value <= item.MasterData.probability)
                {
                    Debug.Log($"[CutIn Queue] {(isEnemy ? "[Enemy] " : "[Player] ")}{item.MasterData.artifactName} 発動！");

                    if (!isEnemy && ArtifactCutInUI.Instance != null)
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

        private void HandleItemBurnOut(ArtifactInstance item, bool isEnemy)
        {
            var targetList = isEnemy ? EnemyEquippedArtifacts : PlayerEquippedArtifacts;

            var coolSpray = targetList.Find(x => !x.IsBurnedOut && x.MasterData.effectType == EffectType.ResetHeatOfItem);
            if (coolSpray != null)
            {
                item.ResetHeat();
                coolSpray.AddHeat(1);
                return;
            }

            TryProcArtifact(targetList, TriggerCondition.OnItemBurnOut, EffectType.DamageOnBurnOut, _ => {
                if (!isEnemy) OnBurnOutDamageToEnemy?.Invoke(1);
            }, isEnemy);
        }

        // ★ 発砲後に呼び出すことで、熱限界を迎えたアイテムを確実に回収・3Dモデル破棄を行う
        public bool ProcessAndExtractBurnedOutItems(bool isEnemy, CylinderController cylinderController)
        {
            var targetList = isEnemy ? EnemyEquippedArtifacts : PlayerEquippedArtifacts;
            bool hasBurnedOut = false;

            for (int i = targetList.Count - 1; i >= 0; i--)
            {
                var item = targetList[i];
                if (item.IsBurnedOut || item.CurrentHeat >= item.MasterData.maxHeat)
                {
                    hasBurnedOut = true;

                    // 3Dモデルを破棄
                    if (item.VisualGameObject != null)
                    {
                        Destroy(item.VisualGameObject);
                    }

                    targetList.RemoveAt(i);
                }
            }

            // プレイヤー側の焼失があった場合のみ、キューを経由して「壊れたか」を表示
            if (hasBurnedOut && !isEnemy && cylinderController != null)
            {
                cylinderController.ShowTextInfo("壊れたか");
            }

            return hasBurnedOut;
        }

        private void CoolAdjacentItems(List<ArtifactInstance> targetList, ArtifactInstance origin, int amount)
        {
            int index = targetList.IndexOf(origin);
            if (index > 0) targetList[index - 1].CoolHeat(amount);
            if (index < targetList.Count - 1) targetList[index + 1].CoolHeat(amount);
        }

        public bool HasEffect(bool isEnemy, EffectType effectType)
        {
            var targetList = isEnemy ? EnemyEquippedArtifacts : PlayerEquippedArtifacts;
            return targetList.Exists(x => !x.IsBurnedOut && x.MasterData.effectType == effectType);
        }

        public (int finalDamage, int reflectDamage) ProcessIncomingDamage(int rawDamage, bool isTargetEnemy)
        {
            var targetList = isTargetEnemy ? EnemyEquippedArtifacts : PlayerEquippedArtifacts;
            var queue = isTargetEnemy ? _enemyDelayedDamageQueue : _playerDelayedDamageQueue;
            int immediateDamage = rawDamage;
            int reflectDamage = 0;

            TryProcArtifact(targetList, TriggerCondition.OnTakeDamage, EffectType.DelayDamage, _ => {
                for (int d = 0; d < rawDamage; d++) queue.Add(3);
                immediateDamage = 0;
            }, isTargetEnemy);

            TryProcArtifact(targetList, TriggerCondition.OnTakeDamage, EffectType.ReflectDamage, _ => {
                reflectDamage = rawDamage;
            }, isTargetEnemy);

            return (immediateDamage, reflectDamage);
        }

        public int OnShotExecuted(bool isEnemy)
        {
            var queue = isEnemy ? _enemyDelayedDamageQueue : _playerDelayedDamageQueue;
            int triggeredDamage = 0;
            for (int i = queue.Count - 1; i >= 0; i--)
            {
                queue[i]--;
                if (queue[i] <= 0)
                {
                    triggeredDamage++;
                    queue.RemoveAt(i);
                }
            }
            return triggeredDamage;
        }

        public void CheckCoolAdjacentOnSelfBlank(bool isEnemy)
        {
            var targetList = isEnemy ? EnemyEquippedArtifacts : PlayerEquippedArtifacts;
            TryProcArtifact(targetList, TriggerCondition.OnShootSelfBlank, EffectType.CoolAdjacent, item => {
                CoolAdjacentItems(targetList, item, (int)item.MasterData.effectValue);
            }, isEnemy);
        }

        public bool CheckPredictNextBullet()
        {
            bool predicted = false;
            TryProcArtifact(PlayerEquippedArtifacts, TriggerCondition.OnTurnStart, EffectType.PredictNext, _ => {
                predicted = true;
            }, isEnemy: false);
            return predicted;
        }

        public bool CheckJamSelfReal(bool isEnemy)
        {
            var targetList = isEnemy ? EnemyEquippedArtifacts : PlayerEquippedArtifacts;
            return TryProcArtifact(targetList, TriggerCondition.OnShootSelfReal, EffectType.JamSelfReal, null, isEnemy);
        }

        public int CheckHealOnSelfBlank(bool isEnemy)
        {
            var targetList = isEnemy ? EnemyEquippedArtifacts : PlayerEquippedArtifacts;
            int heal = 0;
            TryProcArtifact(targetList, TriggerCondition.OnShootSelfBlank, EffectType.AddHp, item => {
                heal = (int)item.MasterData.effectValue;
            }, isEnemy);
            return heal;
        }

        public int ProcessOutgoingDamage(int baseDamage, bool isEnemy)
        {
            var targetList = isEnemy ? EnemyEquippedArtifacts : PlayerEquippedArtifacts;
            int bonus = 0;
            TryProcArtifact(targetList, TriggerCondition.OnAttackReal, EffectType.DamageBoost, item => {
                bonus += (int)item.MasterData.effectValue;
            }, isEnemy);
            return baseDamage + bonus;
        }

        public bool CheckFatalProtection(bool isEnemy)
        {
            var targetList = isEnemy ? EnemyEquippedArtifacts : PlayerEquippedArtifacts;
            bool survived = TryProcArtifact(targetList, TriggerCondition.OnFatalDamage, EffectType.SurviveHP1, null, isEnemy);
            if (!survived)
            {
                survived = TryProcArtifact(targetList, TriggerCondition.OnFatalDamage, EffectType.SurviveAndSeizeInterest, _ => {
                    if (!isEnemy && MoneyManager.Instance != null)
                    {
                        MoneyManager.Instance.SetSeizedInterestFlag();
                    }
                }, isEnemy);
            }
            return survived;
        }

        public bool HasBonusInterestArtifact()
        {
            return PlayerEquippedArtifacts.Exists(x => !x.IsBurnedOut && x.MasterData.effectType == EffectType.BonusInterest);
        }
    }
}