using UnityEngine;

namespace BargainBullet.Core
{
    public class TableSlotManager : MonoBehaviour
    {
        [Header("卓上スロット（最大4枠）")]
        [SerializeField] private Transform[] playerSlots = new Transform[4];   // プレイヤー側スロット4つ
        [SerializeField] private Transform[] opponentSlots = new Transform[4]; // 対戦相手側スロット4つ

        public int MaxSlotCount => 4;

        public Transform GetPlayerSlot(int index)
        {
            if (index >= 0 && index < playerSlots.Length)
                return playerSlots[index];
            return null;
        }

        public Transform GetOpponentSlot(int index)
        {
            if (index >= 0 && index < opponentSlots.Length)
                return opponentSlots[index];
            return null;
        }
    }
}