using UnityEngine;

namespace BargainBullet.Artifacts
{
    public class ArtifactVisual : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color heatedColor = Color.red;
        [SerializeField] private float emissionIntensity = 2f; // 熱限界時の発光強度

        private Material instanceMaterial;

        private void Awake()
        {
            if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
            if (targetRenderer != null)
            {
                // マテリアルのインスタンスを作成して個別に色を変更できるようにする
                instanceMaterial = targetRenderer.material;
            }
        }

        /// <summary>
        /// 熱量に応じた赤色化（0.0 = 通常、1.0 = 限界の赤）
        /// </summary>
        public void UpdateHeatVisual(float heatRatio)
        {
            if (instanceMaterial == null) return;

            // 1. ベースカラーを赤色へ補間
            Color currentColor = Color.Lerp(normalColor, heatedColor, heatRatio);
            instanceMaterial.color = currentColor;

            // 2. 発光（Emission）させて赤く熱を帯びている表現にする
            if (instanceMaterial.HasProperty("_EmissionColor"))
            {
                Color finalEmission = heatedColor * (heatRatio * emissionIntensity);
                instanceMaterial.SetColor("_EmissionColor", finalEmission);
                instanceMaterial.EnableKeyword("_EMISSION");
            }
        }
    }
}