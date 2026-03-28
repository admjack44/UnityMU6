using UnityEngine;

namespace MuOnline.Core
{
    /// <summary>
    /// Eventos de gameplay **local** (cliente / offline). El servidor puede
    /// reemplazar o anular estos flujos publicando <see cref="WorldEvents"/>
    /// y desactivando los sistemas locales correspondientes.
    /// </summary>
    public static class LocalGameplayEvents
    {
        /// <summary>Vida / maná del jugador local cambió.</summary>
        public struct VitalsChanged
        {
            public int Hp, MaxHp, Mp, MaxMp;
        }

        public struct ExpChanged
        {
            public long Exp, ExpMax;
            public int Level;
        }

        public struct ZenChanged
        {
            public long Zen;
        }

        /// <summary>Objetivo de combate seleccionado (null = ninguno).</summary>
        public struct TargetChanged
        {
            public Transform TargetTransform;
        }

        /// <summary>Solicitud de mostrar texto de daño en mundo o UI.</summary>
        public struct DamageFloaterRequested
        {
            public Vector3 WorldPosition;
            public int Amount;
            public bool IsCritical;
            public bool IsPlayerSource;
        }

        public struct InventoryUpdated
        {
            public int SlotCount;
        }

        public struct AutoBattleToggled
        {
            public bool Enabled;
        }

        public struct SkillUsedLocal
        {
            public int SkillIndex;
            public ushort SkillId;
        }

        public struct ChatLocal
        {
            public string RichTextLine;
        }
    }
}
