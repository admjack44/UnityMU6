using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using MuOnline.Gameplay.AutoBattle;
using MuOnline.Gameplay.Combat;
using MuOnline.Gameplay.Skills;
using MuOnline.UI.Windows;

namespace MuOnline.UI.Gameplay
{
    /// <summary>Conecta botones generados por <see cref="GameScreen"/> con sistemas de combate.</summary>
    public class GameplayActionBinder : MonoBehaviour
    {
        [SerializeField] private CombatController combat;
        [SerializeField] private SkillController skills;
        [SerializeField] private AutoBattleController autoBattle;
        [SerializeField] private Transform hudRoot;

        public void Wire(CombatController c, SkillController sk, AutoBattleController auto, Transform hudCanvasRoot)
        {
            combat = c;
            skills = sk;
            autoBattle = auto;
            if (hudCanvasRoot != null) hudRoot = hudCanvasRoot;
        }

        void Start()
        {
            if (hudRoot == null)
            {
                var cv = FindFirstObjectByType<Canvas>();
                if (cv != null) hudRoot = cv.transform;
            }

            BindByName("AttackMain", () =>
            {
                if (combat != null) combat.TryBasicAttack();
            });
            for (int i = 0; i < 5; i++)
            {
                int idx = i;
                BindByName($"Sk{idx + 1}", () =>
                {
                    if (skills != null) skills.TryUseSkill(idx);
                });
            }

            BindByName("Inv", () =>
            {
                var m = UIWindowManager.Instance;
                if (m != null) m.ToggleInventory();
            });
            BindByName("Man", () =>
            {
                var m = UIWindowManager.Instance;
                if (m != null) m.ToggleCharacterStats();
            });

            BuildAutoToggleIfNeeded();
        }

        void BindByName(string childName, UnityEngine.Events.UnityAction action)
        {
            if (hudRoot == null || action == null) return;
            foreach (var t in hudRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != childName) continue;
                var btn = t.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(action);
                }
                break;
            }
        }

        void BuildAutoToggleIfNeeded()
        {
            if (autoBattle == null || hudRoot == null) return;
            if (hudRoot.GetComponentsInChildren<Transform>(true).Any(x => x.name == "AutoPlayToggle"))
                return;

            var go = new GameObject("AutoPlayToggle");
            go.transform.SetParent(hudRoot, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-220f, 320f);
            rt.sizeDelta = new Vector2(140f, 36f);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.06f, 0.04f, 0.1f, 0.92f);
            var btn = go.AddComponent<Button>();
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var tr = labelGo.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = tr.offsetMax = Vector2.zero;
            var tmp = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.fontSize = 14;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.text = "AUTO: OFF";
            tmp.color = new Color(0.9f, 0.75f, 0.35f);

            btn.onClick.AddListener(() =>
            {
                autoBattle.ToggleAuto();
                tmp.text = autoBattle.IsAutoEnabled ? "AUTO: ON" : "AUTO: OFF";
            });
        }
    }
}
