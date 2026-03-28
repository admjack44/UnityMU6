using UnityEngine;

namespace MuOnline.UI
{
    /// <summary>
    /// Paleta de colores estilo MU Immortal.
    /// </summary>
    public static class MuUITheme
    {
        // ── Colores principales MU Immortal ──────────────────────────────────
        public static readonly Color BackgroundDark   = new Color(0.02f, 0.01f, 0.06f, 1f);   // negro violeta profundo
        public static readonly Color PanelBackground  = new Color(0.05f, 0.03f, 0.12f, 0.96f); // morado muy oscuro
        public static readonly Color PanelBackground2 = new Color(0.08f, 0.04f, 0.18f, 0.92f); // morado oscuro
        public static readonly Color PanelBorder      = new Color(0.55f, 0.35f, 0.85f, 1f);    // morado brillante
        public static readonly Color PanelBorderGold  = new Color(0.85f, 0.68f, 0.15f, 1f);    // dorado

        // Dorado MU Immortal
        public static readonly Color GoldPrimary      = new Color(1f,    0.82f, 0.18f, 1f);
        public static readonly Color GoldBright       = new Color(1f,    0.92f, 0.45f, 1f);
        public static readonly Color GoldDark         = new Color(0.65f, 0.48f, 0.05f, 1f);

        // Morados
        public static readonly Color PurplePrimary    = new Color(0.55f, 0.25f, 0.95f, 1f);
        public static readonly Color PurpleDark       = new Color(0.25f, 0.10f, 0.45f, 1f);
        public static readonly Color PurpleLight      = new Color(0.72f, 0.50f, 1.00f, 1f);

        // Textos
        public static readonly Color TextPrimary      = new Color(0.95f, 0.92f, 0.80f, 1f);   // crema cálido
        public static readonly Color TextSecondary    = new Color(0.60f, 0.55f, 0.75f, 1f);   // gris violáceo
        public static readonly Color TextError        = new Color(1f,    0.25f, 0.25f, 1f);
        public static readonly Color TextSuccess      = new Color(0.30f, 1f,    0.45f, 1f);
        public static readonly Color TextWarning      = new Color(1f,    0.80f, 0.15f, 1f);
        public static readonly Color TextGold         = new Color(1f,    0.82f, 0.18f, 1f);
        public static readonly Color TextPurple       = new Color(0.72f, 0.50f, 1.00f, 1f);

        // Botones
        public static readonly Color ButtonNormal     = new Color(0.12f, 0.06f, 0.25f, 1f);   // morado oscuro
        public static readonly Color ButtonHover      = new Color(0.22f, 0.10f, 0.42f, 1f);
        public static readonly Color ButtonPressed    = new Color(0.06f, 0.03f, 0.14f, 1f);
        public static readonly Color ButtonGold       = new Color(0.18f, 0.14f, 0.04f, 1f);
        public static readonly Color ButtonGoldHover  = new Color(0.28f, 0.22f, 0.06f, 1f);

        // Inputs
        public static readonly Color InputBackground  = new Color(0.04f, 0.02f, 0.10f, 1f);
        public static readonly Color InputBorder      = new Color(0.35f, 0.20f, 0.60f, 1f);

        // Stats
        public static readonly Color HpColor          = new Color(0.90f, 0.15f, 0.15f, 1f);
        public static readonly Color ManaColor        = new Color(0.15f, 0.35f, 1.00f, 1f);
        public static readonly Color StaminaColor     = new Color(1.00f, 0.65f, 0.10f, 1f);

        // ── Tamaños de fuente ────────────────────────────────────────────────
        public const float FontSizeTitle   = 40f;
        public const float FontSizeNormal  = 16f;
        public const float FontSizeSmall   = 13f;
        public const float FontSizeTiny    = 11f;

        // ── Color por clase ──────────────────────────────────────────────────
        public static Color GetClassColor(Core.CharacterClass cls) => cls switch
        {
            Core.CharacterClass.DarkKnight     => new Color(0.90f, 0.20f, 0.20f),
            Core.CharacterClass.DarkWizard     => new Color(0.45f, 0.45f, 1.00f),
            Core.CharacterClass.FairyElf       => new Color(0.30f, 0.95f, 0.45f),
            Core.CharacterClass.MagicGladiator => new Color(1.00f, 0.65f, 0.10f),
            Core.CharacterClass.DarkLord       => new Color(0.72f, 0.20f, 0.95f),
            Core.CharacterClass.Summoner       => new Color(0.95f, 0.55f, 0.85f),
            Core.CharacterClass.RageFighter    => new Color(1.00f, 0.40f, 0.10f),
            _                                  => Color.white
        };

        public static string GetClassIcon(Core.CharacterClass cls) => cls switch
        {
            Core.CharacterClass.DarkKnight     => "⚔",
            Core.CharacterClass.DarkWizard     => "✦",
            Core.CharacterClass.FairyElf       => "🏹",
            Core.CharacterClass.MagicGladiator => "⚡",
            Core.CharacterClass.DarkLord       => "👑",
            Core.CharacterClass.Summoner       => "🌙",
            Core.CharacterClass.RageFighter    => "🔥",
            _                                  => "?"
        };
    }
}
