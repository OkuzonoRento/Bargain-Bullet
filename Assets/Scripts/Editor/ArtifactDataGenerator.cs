#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace BargainBullet.Artifacts
{
    public class ArtifactDataGenerator
    {
        [MenuItem("BargainBullet/Generate All Artifact Assets")]
        public static void GenerateAllArtifacts()
        {
            string path = "Assets/Resources/Artifacts";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            CreateAsset(path, "ART_001", "", Rarity.Rare, ArtifactType.Permanent, 999, TriggerCondition.OnTakeDamage, 0.3f, EffectType.DelayDamage, 1f, "被弾時30%の確率でダメージを無効化し、3ターン後に受ける");
            CreateAsset(path, "ART_002", "", Rarity.Uncommon, ArtifactType.Permanent, 999, TriggerCondition.OnShootSelfBlank, 0.25f, EffectType.CoolAdjacent, 1f, "自分に空砲を撃った時25%の確率で隣接アーティファクトの熱量を1段階冷やす");
            CreateAsset(path, "ART_003", "", Rarity.Common, ArtifactType.Disposable, 2, TriggerCondition.OnTurnStart, 0.5f, EffectType.PredictNext, 1f, "ターン開始時50%の確率で次の弾種が判明する（許熱量2 / 発動ごとに熱+1）");
            CreateAsset(path, "ART_004", "", Rarity.Uncommon, ArtifactType.Disposable, 1, TriggerCondition.OnShootSelfReal, 0.3f, EffectType.JamSelfReal, 1f, "自分に実弾を撃った時30%の確率でダメージ無効＆ターン継続（許熱量1 / 1回使用で焼失）");
            CreateAsset(path, "ART_005", "", Rarity.Rare, ArtifactType.Disposable, 1, TriggerCondition.OnTakeDamage, 0.4f, EffectType.ReflectDamage, 1f, "被弾時40%の確率で受けたダメージを相手にも与える（許熱量1 / 1回使用で焼失）");
            CreateAsset(path, "ART_006", "", Rarity.Common, ArtifactType.Disposable, 3, TriggerCondition.OnShootSelfBlank, 0.33f, EffectType.AddHp, 1f, "自分に空砲を撃った時33%の確率でHP1回復（許熱量3 / 発動ごとに熱+1）");
            CreateAsset(path, "ART_007", "", Rarity.Uncommon, ArtifactType.Disposable, 2, TriggerCondition.OnAttackReal, 0.5f, EffectType.DamageBoost, 1f, "相手に実弾を当てた時50%の確率でダメージ+1（許熱量2 / 発動ごとに熱+1）");
            CreateAsset(path, "ART_008", "", Rarity.Epic, ArtifactType.Disposable, 1, TriggerCondition.OnFatalDamage, 1f, EffectType.SurviveHP1, 1f, "致死ダメージ時確定でHP1で耐える（許熱量1 / 発動後焼失）");
            CreateAsset(path, "ART_009", "", Rarity.Rare, ArtifactType.Permanent, 999, TriggerCondition.OnBattleWin, 1f, EffectType.BonusInterest, 0.05f, "勝利時の利息計算時確定で利息率+5%");
            CreateAsset(path, "ART_010", "", Rarity.Uncommon, ArtifactType.Permanent, 999, TriggerCondition.OnThreeBlanks, 1f, EffectType.ForceRealBullet, 1f, "3連続で空砲が出た後確定で次の弾を実弾化");
            CreateAsset(path, "ART_011", "", Rarity.Rare, ArtifactType.Disposable, 1, TriggerCondition.OnOpponentShootSelfBlank, 0.4f, EffectType.StealTurn, 1f, "相手が自分撃ち（空砲）に成功した時、40%の確率で相手の追加ターンをキャンセルし自分のターンにする（許熱量1）");
            CreateAsset(path, "ART_012", "", Rarity.Rare, ArtifactType.Disposable, 1, TriggerCondition.OnFatalDamage, 1f, EffectType.SurviveAndSeizeInterest, 10f, "致死ダメージ時確定でHP1で耐えるが次回利息が激減（許熱量1）");
            CreateAsset(path, "ART_013", "", Rarity.Common, ArtifactType.Disposable, 3, TriggerCondition.OnShootSelfReal, 1f, EffectType.InsuranceByDamage, 30f, "自分に実弾を撃ってしまった時、受けたダメージ量×$30の保険金を得る（許熱量3）");
            CreateAsset(path, "ART_014", "", Rarity.Uncommon, ArtifactType.Disposable, 1, TriggerCondition.OnHeatMax, 1f, EffectType.ResetHeatOfItem, 0f, "消滅型アーティファクト1つが熱限界を迎えた際、その熱を完全に0までリセットし自身が消滅する（許熱量1 / 1回使い切り）");
            CreateAsset(path, "ART_015", "", Rarity.Rare, ArtifactType.Permanent, 999, TriggerCondition.OnItemBurnOut, 1f, EffectType.DamageOnBurnOut, 1f, "手元の消滅型アーティファクトが熱で焼失した瞬間、確定で相手に1ダメージを与える");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateAsset(string folderPath, string id, string name, Rarity rarity, ArtifactType type, int maxHeat, TriggerCondition trigger, float prob, EffectType effect, float val, string desc)
        {
            string assetPath = $"{folderPath}/{id}.asset";
            ArtifactData data = AssetDatabase.LoadAssetAtPath<ArtifactData>(assetPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<ArtifactData>();
                AssetDatabase.CreateAsset(data, assetPath);
            }

            data.id = id;
            data.artifactName = name;
            data.rarity = rarity;
            data.artifactType = type;
            data.maxHeat = maxHeat;
            data.triggerCondition = trigger;
            data.probability = prob;
            data.effectType = effect;
            data.effectValue = val;
            data.description = desc;

            EditorUtility.SetDirty(data);
        }
    }
}
#endif