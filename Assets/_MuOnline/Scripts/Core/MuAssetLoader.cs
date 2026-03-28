using System.Collections.Generic;
using UnityEngine;

namespace MuOnline.Core
{
    /// <summary>
    /// Carga y cachea los Sprites originales del cliente MU Season 6.
    /// Uso: MuAssetLoader.Get("Login/lo_mu_logo")
    /// </summary>
    public static class MuAssetLoader
    {
        private const string BASE_PATH = "MuAssets";
        private static readonly Dictionary<string, Sprite> _cache = new();

        // ── Helpers de carga ─────────────────────────────────────────────────

        public static Sprite Get(string relativePath)
        {
            if (_cache.TryGetValue(relativePath, out var cached))
                return cached;

            var sprite = Resources.Load<Sprite>($"{BASE_PATH}/{relativePath}");

            if (sprite == null)
                Debug.LogWarning($"[MuAssets] No encontrado: {BASE_PATH}/{relativePath}");
            else
                _cache[relativePath] = sprite;

            return sprite;
        }

        public static Sprite GetOrNull(string relativePath) => Get(relativePath);

        /// Aplica un sprite a una Image, si existe. Si no, usa el color fallback.
        public static bool ApplySprite(UnityEngine.UI.Image img, string path,
            Color? fallbackColor = null)
        {
            var sprite = Get(path);
            if (sprite != null)
            {
                img.sprite = sprite;
                img.color  = Color.white;
                return true;
            }
            if (fallbackColor.HasValue)
                img.color = fallbackColor.Value;
            return false;
        }

        public static void ClearCache() => _cache.Clear();

        // ── Paths predefinidos ────────────────────────────────────────────────

        /// Assets de Interface_out (login panel, loading bar, botones HUD...)
        public static class Login
        {
            // Interface_out: panel frame, logo pequeño, fondos de arte
            public const string Logo        = "Login/lo_mu_logo";
            public const string Panel       = "Login/login_me";
            public const string Background  = "Login/login_back";
            public const string BgArt1      = "Login/lo_back_im01";
            public const string BgArt2      = "Login/lo_back_im02";
            public const string BgArt3      = "Login/lo_back_im03";
            public const string BgArt4      = "Login/lo_back_im04";
            public const string BgArt5      = "Login/lo_back_im05";
            public const string BgArt6      = "Login/lo_back_im06";
            public const string BgFlat1     = "Login/lo_back_01";
            public const string BgFlat2     = "Login/lo_back_02";
            public const string BtnConnect  = "Login/b_connect";
            public const string LoadBg1     = "Login/LSBg01";
            public const string LoadBg2     = "Login/LSBg02";
            public const string LoadBg3     = "Login/LSBg03";
            public const string LoadBg4     = "Login/LSBg04";
            public const string MuTitle     = "Login/MU_TITLE";
            public const string MuTitleBlue = "Login/MU_TITLE_Blue";

            public static string RandomBgArt()
            {
                var arts = new[] { BgArt1, BgArt2, BgArt3, BgArt4, BgArt5, BgArt6 };
                return arts[Random.Range(0, arts.Length)];
            }
        }

        /// Assets de Logo_out (logo oficial, pantallas de carga, UI de cuenta, retratos)
        public static class LogoScreen
        {
            // Fondos del Login screen original
            public const string LoginBack1    = "Logo/Login_Back01";
            public const string LoginBack2    = "Logo/Login_Back02";
            public const string NewLoginBack1 = "Logo/New_Login_Back01";
            public const string NewLoginBack2 = "Logo/New_Login_Back02";

            // Pantallas de carga del intro
            public const string Loading1      = "Logo/Loading01";
            public const string Loading2      = "Logo/Loading02";
            public const string Loading3      = "Logo/Loading03";

            // Logos MU oficiales
            public const string MuLogoTga     = "Logo/MU-logo";
            public const string MuLogoBlue    = "Logo/MuBlue_logo";
            public const string MuLogoGold    = "Logo/MU-logo_g";
            public const string MuLogoBlueGfx = "Logo/MuBlue_logo_g";
            public const string MuLogoSmall   = "Logo/mulogo_01";
            public const string Titel1        = "Logo/titel01";
            public const string Titel2        = "Logo/titel02";

            // Fondos decorativos
            public const string Back1         = "Logo/UI/back1";
            public const string Back2         = "Logo/UI/back2";
            public const string Back3         = "Logo/UI/back3";
            public const string Back4         = "Logo/UI/back4";
            public const string Everyone      = "Logo/UI/Everyone";

            // Paneles de cuenta (UI original Login)
            public const string AccountPanel      = "Logo/UI/0Account";
            public const string AccountPanelNew   = "Logo/UI/0Account_new";
            public const string NewAccountPanel1  = "Logo/UI/0New_Account01";
            public const string NewAccountPanel2  = "Logo/UI/0New_Account02";
            public const string TextBox           = "Logo/UI/0Text_Box";
            public const string Box               = "Logo/UI/0Box";
            public const string BtnOn1            = "Logo/UI/0On_Botton";
            public const string BtnOn2            = "Logo/UI/0On_Botton2";

            // Overlays de interfaz
            public const string Interface1    = "Logo/UI/Interface01";
            public const string Interface2    = "Logo/UI/Interface02";
            public const string Interface3    = "Logo/UI/Interface03";
            public const string Interface4    = "Logo/UI/Interface04";

            // Botones OK/Delete
            public const string OkBtn1        = "Logo/UI/Ok01";
            public const string OkBtn2        = "Logo/UI/Ok02";
            public const string DeleteBtn1    = "Logo/UI/Delete01";
            public const string DeleteBtn2    = "Logo/UI/Delete02";

            // Pantalla selección de personaje (nueva UI)
            public const string NewCharBg1    = "Logo/UI/New_Character001";
            public const string NewCharBg2    = "Logo/UI/New_Character002";
            public const string NewCharBg3    = "Logo/UI/New_Character003";
            public const string NewCharBg4    = "Logo/UI/New_Character004";
            // Fondos de selección de personaje (S6)
            public const string CharSelectBg1 = "CharSelect/CharSelectBg1";
            public const string CharSelectBg2 = "CharSelect/CharSelectBg2";
            public const string CharSelectBg3 = "CharSelect/CharSelectBg3";
            public const string NewCharFrame1 = "Logo/UI/New_Character01";
            public const string NewCharFrame2 = "Logo/UI/New_Character02";
            public const string NewCharExtra1 = "Logo/UI/New_Character201";
            public const string NewCharExtra2 = "Logo/UI/New_Character202";
            public const string NewCharOk     = "Logo/UI/New_Character_Ok";
            public const string NewCharCancel = "Logo/UI/New_Character_Cancel";

            // Helpers de aleatoriedad
            public static string RandomLoginBg()
            {
                var bgs = new[] { LoginBack1, LoginBack2, NewLoginBack1, NewLoginBack2 };
                return bgs[Random.Range(0, bgs.Length)];
            }

            public static string RandomLoadingBg()
            {
                var bgs = new[] { Loading1, Loading2, Loading3 };
                return bgs[Random.Range(0, bgs.Length)];
            }
        }

        /// Retratos de clase para character select (c01=Wizard, c02=DK, c03=Elf, etc.)
        public static class CharPortraits
        {
            public const string Wizard          = "Logo/Characters/c01";
            public const string DarkKnight      = "Logo/Characters/c02";
            public const string FairyElf        = "Logo/Characters/c03";
            public const string MagicGladiator  = "Logo/Characters/c04";
            public const string DarkLord        = "Logo/Characters/c06";
            public const string Summoner        = "Logo/Characters/c07";

            public static string ByClass(Core.CharacterClass cls) => cls switch
            {
                Core.CharacterClass.DarkKnight     => DarkKnight,
                Core.CharacterClass.DarkWizard     => Wizard,
                Core.CharacterClass.FairyElf       => FairyElf,
                Core.CharacterClass.MagicGladiator => MagicGladiator,
                Core.CharacterClass.DarkLord       => DarkLord,
                Core.CharacterClass.Summoner       => Summoner,
                _                                  => DarkKnight
            };
        }

        /// Imágenes de personaje en pantalla de selección (pose idle y seleccionada)
        /// Fuente: Interface_out im1-8 (resolución completa, renderizado en juego)
        public static class CharSelect
        {
            // Idle = pose en reposo | Selected = pose cuando está seleccionado
            public const string WizardIdle          = "CharSelect/Wizard_Idle";
            public const string WizardSelected      = "CharSelect/Wizard_Selected";
            public const string DarkKnightIdle      = "CharSelect/DarkKnight_Idle";
            public const string DarkKnightSelected  = "CharSelect/DarkKnight_Selected";
            public const string FairyElfIdle        = "CharSelect/FairyElf_Idle";
            public const string FairyElfSelected    = "CharSelect/FairyElf_Selected";
            public const string MagicGladIdle       = "CharSelect/MagicGladiator_Idle";
            public const string MagicGladSelected   = "CharSelect/MagicGladiator_Selected";
            public const string DarkLordIdle        = "CharSelect/DarkLord_Idle";
            public const string DarkLordSelected    = "CharSelect/DarkLord_Selected";
            public const string SummonerIdle        = "CharSelect/Summoner_Idle";
            public const string SummonerSelected    = "CharSelect/Summoner_Selected";
            public const string RageFighterIdle     = "CharSelect/RageFighter_Idle";
            public const string RageFighterSelected = "CharSelect/RageFighter_Selected";

            // Fondos de la pantalla de selección
            public const string BgFrame1 = "Logo/UI/New_Character01";
            public const string BgFrame2 = "Logo/UI/New_Character02";
            public const string BtnOk    = "Logo/UI/New_Character_Ok";
            public const string BtnCancel= "Logo/UI/New_Character_Cancel";

            public static string IdleByClass(Core.CharacterClass cls) => cls switch
            {
                Core.CharacterClass.DarkKnight     => DarkKnightIdle,
                Core.CharacterClass.DarkWizard     => WizardIdle,
                Core.CharacterClass.FairyElf       => FairyElfIdle,
                Core.CharacterClass.MagicGladiator => MagicGladIdle,
                Core.CharacterClass.DarkLord       => DarkLordIdle,
                Core.CharacterClass.Summoner       => SummonerIdle,
                Core.CharacterClass.RageFighter    => RageFighterIdle,
                _                                  => DarkKnightIdle
            };

            public static string SelectedByClass(Core.CharacterClass cls) => cls switch
            {
                Core.CharacterClass.DarkKnight     => DarkKnightSelected,
                Core.CharacterClass.DarkWizard     => WizardSelected,
                Core.CharacterClass.FairyElf       => FairyElfSelected,
                Core.CharacterClass.MagicGladiator => MagicGladSelected,
                Core.CharacterClass.DarkLord       => DarkLordSelected,
                Core.CharacterClass.Summoner       => SummonerSelected,
                Core.CharacterClass.RageFighter    => RageFighterSelected,
                _                                  => DarkKnightSelected
            };
        }

        /// Botones de HUD (ok, cancel, close, menú)
        public static class UIButtons
        {
            public const string Ok          = "HUD/ok";
            public const string Ok2         = "HUD/ok2";
            public const string Cancel      = "HUD/cancel";
            public const string Cancel2     = "HUD/cancel2";
            public const string Exit1       = "HUD/exit_01";
            public const string Exit2       = "HUD/exit_02";
            public const string MsgTop      = "HUD/newui_msgbox_top";
            public const string MsgMiddle   = "HUD/newui_msgbox_middle";
            public const string MsgBottom   = "HUD/newui_msgbox_bottom";
            public const string MsgBack     = "HUD/newui_msgbox_back";
            public const string WinTitleBar = "HUD/win_titlebar";
            public const string WinScrollBar= "HUD/win_scrollbar";
            public const string WinButton   = "HUD/win_button";
            public const string MenuBt1     = "HUD/newui_menu_Bt01";
            public const string MenuBt2     = "HUD/newui_menu_Bt02";
            public const string MenuBt3     = "HUD/newui_menu_Bt03";
            public const string MenuBt4     = "HUD/newui_menu_Bt04";
        }

        public static class Buttons
        {
            public const string OK         = "Buttons/newui_button_ok";
            public const string Cancel     = "Buttons/newui_button_cancel";
            public const string Close      = "Buttons/newui_button_close";
            public const string Create     = "Buttons/b_create";
            public const string Delete     = "Buttons/b_delete";
            public const string Connect    = "Login/b_connect";   // alias acceso rápido
            public const string OkSmall1   = "Buttons/m_b_ok1";
            public const string OkSmall2   = "Buttons/m_b_ok2";
            public const string OkSmall3   = "Buttons/m_b_ok3";
            public const string NoSmall1   = "Buttons/m_b_no1";
            public const string NoSmall2   = "Buttons/m_b_no2";
            public const string NoSmall3   = "Buttons/m_b_no3";
            public const string WinButton  = "Buttons/win_button";
        }

        public static class HUD
        {
            public const string MainBar    = "HUD/in_main";
            public const string MainBar2   = "HUD/in_main2";
            public const string MainBarNew = "HUD/in_main-New";
            public const string MainBar2New= "HUD/in_main2-New";
            public const string Bar        = "HUD/in_bar";
            public const string Deco       = "HUD/in_deco";
            public const string Frame      = "HUD/frame";
            public const string MiniFrame  = "HUD/Miniframe";
            public const string SkillBar   = "HUD/BattleSkill";
            public const string Progress   = "HUD/Progress";
            public const string ProgBack   = "HUD/Progress_Back";
            public const string Attack     = "HUD/i_attack";
            public const string Defense    = "HUD/i_defense";
        }

        public static class Panels
        {
            public static string BackPanel(int index) => $"Panels/backpanel{index}";
            public const string Inventory0     = "Panels/inventorypanel0";
            public const string Inventory1     = "Panels/inventorypanel1";
            public const string ItemBack       = "Panels/itembackpanel";
            public const string MiniInventory  = "Panels/miniinventorypanel";
            public const string MessageBack    = "Panels/message_back";
            public const string WinTitleBar    = "Panels/win_titlebar";
            public const string WinScrollBar   = "Panels/win_scrollbar";
        }

        public static class Maps
        {
            public const string Lorencia   = "Maps/Lorencia";
            public const string Devias     = "Maps/Devias";
            public const string Noria      = "Maps/Noria";
            public const string Dungeon    = "Maps/Dungeon";
            public const string Atlans     = "Maps/Atlans";
            public const string Tarkan     = "Maps/Tarkan";
            public const string Icarus     = "Maps/Icarus";
            public const string LostTower  = "Maps/LostTower";

            public static string ByMapId(int mapId) => mapId switch
            {
                0  => Lorencia,
                1  => Dungeon,
                2  => Devias,
                3  => Noria,
                4  => LostTower,
                7  => Atlans,
                8  => Tarkan,
                10 => Icarus,
                _  => Lorencia
            };
        }

        public static class Cursors
        {
            public const string Normal  = "Cursors/Cursor";
            public const string Attack  = "Cursors/CursorAttack";
            public const string Attack2 = "Cursors/CursorAttack2";
            public const string Get     = "Cursors/CursorGet";
            public const string Talk    = "Cursors/CursorTalk";
            public const string NoMove  = "Cursors/CursorDontMove";
        }
    }
}
