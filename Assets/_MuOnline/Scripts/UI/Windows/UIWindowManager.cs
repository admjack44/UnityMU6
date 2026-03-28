using System.Collections.Generic;
using UnityEngine;

namespace MuOnline.UI.Windows
{
    /// <summary>Gestor central de ventanas modales / paneles (inventario, stats, etc.).</summary>
    public class UIWindowManager : MonoBehaviour
    {
        public static UIWindowManager Instance { get; private set; }

        [SerializeField] private InventoryWindow inventoryWindow;
        [SerializeField] private CharacterStatsWindow statsWindow;

        public void AssignWindows(InventoryWindow inv, CharacterStatsWindow stats)
        {
            inventoryWindow = inv;
            statsWindow = stats;
        }
        [SerializeField] private KeyCode inventoryKey = KeyCode.I;
        [SerializeField] private KeyCode statsKey = KeyCode.C;

        readonly Stack<GameObject> _modalStack = new();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (Input.GetKeyDown(inventoryKey)) ToggleInventory();
            if (Input.GetKeyDown(statsKey)) ToggleCharacterStats();
        }

        public void ToggleInventory()
        {
            if (inventoryWindow == null) return;
            bool v = !inventoryWindow.gameObject.activeSelf;
            inventoryWindow.gameObject.SetActive(v);
        }

        public void ToggleCharacterStats()
        {
            if (statsWindow == null) return;
            bool v = !statsWindow.gameObject.activeSelf;
            statsWindow.gameObject.SetActive(v);
        }

        public void RegisterModal(GameObject go)
        {
            if (go != null && go.activeSelf) _modalStack.Push(go);
        }

        public void CloseTop()
        {
            if (_modalStack.Count == 0) return;
            var g = _modalStack.Pop();
            if (g != null) g.SetActive(false);
        }
    }
}
