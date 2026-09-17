using System;

namespace BargainBullet.Artifacts
{
    public class ArtifactInstance
    {
        public ArtifactData MasterData { get; private set; }
        public int CurrentHeat { get; private set; }
        public bool IsBurnedOut { get; private set; }

        public event Action<ArtifactInstance, float> OnHeatChanged;
        public event Action<ArtifactInstance> OnBurnedOut;

        public ArtifactInstance(ArtifactData masterData)
        {
            MasterData = masterData;
            CurrentHeat = 0;
            IsBurnedOut = false;
        }

        public void AddHeat(int amount = 1)
        {
            if (IsBurnedOut || MasterData.artifactType == ArtifactType.Permanent) return;

            CurrentHeat += amount;
            float heatRatio = (float)CurrentHeat / MasterData.maxHeat;
            OnHeatChanged?.Invoke(this, heatRatio);

            if (CurrentHeat >= MasterData.maxHeat)
            {
                IsBurnedOut = true;
                OnBurnedOut?.Invoke(this);
            }
        }

        public void CoolHeat(int amount)
        {
            if (IsBurnedOut) return;
            CurrentHeat = Math.Max(0, CurrentHeat - amount);
            float heatRatio = (float)CurrentHeat / MasterData.maxHeat;
            OnHeatChanged?.Invoke(this, heatRatio);
        }

        public void ResetHeat()
        {
            if (IsBurnedOut) return;
            CurrentHeat = 0;
            OnHeatChanged?.Invoke(this, 0f);
        }
    }
}