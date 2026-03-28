using UnityEngine;

namespace MuOnline.Gameplay
{
    /// <summary>Stats mínimas de enemigo para mitigación y UI.</summary>
    public class EnemyStatProfile : MonoBehaviour
    {
        [SerializeField] private int defense = 4;
        [SerializeField] private int expReward = 35;
        [SerializeField] private int zenReward = 12;

        public int Defense => defense;
        public int ExpReward => expReward;
        public int ZenReward => zenReward;
    }
}
