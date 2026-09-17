using UnityEngine;

namespace BargainBullet.Artifacts
{
    [CreateAssetMenu(fileName = "NewArtifactData", menuName = "BargainBullet/Artifact Data")]
    public class ArtifactData : ScriptableObject
    {
        [Header("基本情報")]
        public string id;
        public string artifactName;
        public Rarity rarity;
        public ArtifactType artifactType;
        public Sprite icon;                       // UI用

        [Header("3Dモデル & 演出")]
        public GameObject artifactPrefab;         // 卓上に置く3Dモデル（ArtifactVisualをアタッチ）
        public Sprite cutInIllustration;         // クローバーピット風カットイン用画像
        public GameObject activationVfxPrefab;   // 発動時VFX
        public GameObject burnOutVfxPrefab;      // 焼失時VFX
        public AudioClip activationSfx;          // 発動SE
        public AudioClip burnOutSfx;             // 消滅SE（ジュッという音）

        [Header("熱・耐久値設定")]
        public int maxHeat = 1;

        [Header("発動ロジック")]
        public TriggerCondition triggerCondition;
        [Range(0f, 1f)] public float probability = 1f;
        public EffectType effectType;
        public float effectValue = 1f;

        [Header("説明")]
        [TextArea(2, 4)] public string description;
    }
}