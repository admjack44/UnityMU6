using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MuOnline.Core;
using MuOnline.Gameplay.Inventory;
using MuOnline.UI;

namespace MuOnline.UI.Windows
{
    /// <summary>Ventana de inventario en grid simple (UGUI).</summary>
    public class InventoryWindow : MonoBehaviour
    {
        [SerializeField] private InventoryController source;
        [SerializeField] private Transform slotRoot;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private int columns = 8;

        TextMeshProUGUI[] _labels;

        void OnEnable()
        {
            EventBus.Subscribe<LocalGameplayEvents.InventoryUpdated>(OnInventoryEvent);
            Refresh();
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<LocalGameplayEvents.InventoryUpdated>(OnInventoryEvent);
        }

        void OnInventoryEvent(LocalGameplayEvents.InventoryUpdated e) => Refresh();

        public void Bind(InventoryController inv) => source = inv;

        public void Refresh()
        {
            if (source == null) return;
            EnsureSlots(source.SlotCount);
            var slots = source.Slots;
            for (int i = 0; i < slots.Length; i++)
            {
                var s = slots[i];
                _labels[i].text = s.IsEmpty ? "" : $"{s.DisplayName}\nx{s.Count}";
            }
        }

        void EnsureSlots(int count)
        {
            if (slotRoot == null)
            {
                BuildFallbackLayout(count);
                return;
            }

            if (_labels != null && _labels.Length == count) return;

            foreach (Transform c in slotRoot)
                Destroy(c.gameObject);

            _labels = new TextMeshProUGUI[count];
            for (int i = 0; i < count; i++)
            {
                var go = slotPrefab != null
                    ? Instantiate(slotPrefab, slotRoot)
                    : CreateSlot(slotRoot, i);
                _labels[i] = go.GetComponentInChildren<TextMeshProUGUI>();
                if (_labels[i] == null)
                {
                    var tgo = new GameObject("Txt");
                    tgo.transform.SetParent(go.transform, false);
                    var rt = tgo.AddComponent<RectTransform>();
                    UIBuilder.StretchFill(rt);
                    _labels[i] = tgo.AddComponent<TextMeshProUGUI>();
                    _labels[i].fontSize = 11;
                    _labels[i].alignment = TextAlignmentOptions.Center;
                    _labels[i].color = MuUITheme.TextPrimary;
                }
            }
        }

        void BuildFallbackLayout(int count)
        {
            var root = transform.Find("SlotRoot");
            if (root == null)
            {
                var p = new GameObject("SlotRoot");
                p.transform.SetParent(transform, false);
                var prt = p.AddComponent<RectTransform>();
                prt.anchorMin = new Vector2(0.05f, 0.12f);
                prt.anchorMax = new Vector2(0.95f, 0.88f);
                prt.offsetMin = prt.offsetMax = Vector2.zero;
                var grid = p.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(72f, 72f);
                grid.spacing = new Vector2(6f, 6f);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = columns;
                slotRoot = p.transform;
            }

            slotPrefab = null;
            EnsureSlots(count);
        }

        static GameObject CreateSlot(Transform parent, int index)
        {
            var go = new GameObject($"Slot_{index}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(72f, 72f);
            var img = go.AddComponent<Image>();
            img.color = MuUITheme.PanelBackground;
            go.AddComponent<Outline>().effectColor = MuUITheme.PanelBorderGold;
            return go;
        }
    }
}
