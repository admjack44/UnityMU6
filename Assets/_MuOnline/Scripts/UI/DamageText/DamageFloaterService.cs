using System.Collections;
using TMPro;
using UnityEngine;
using MuOnline.Core;

namespace MuOnline.UI.DamageText
{
    /// <summary>Spawna texto de daño world-space; pool simple.</summary>
    public class DamageFloaterService : MonoBehaviour
    {
        [SerializeField] private int poolSize = 24;
        [SerializeField] private float floatSpeed = 1.35f;
        [SerializeField] private float lifetime = 0.85f;

        TextMeshPro[] _pool;
        int _idx;

        void OnEnable()
        {
            EventBus.Subscribe<LocalGameplayEvents.DamageFloaterRequested>(OnDamage);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<LocalGameplayEvents.DamageFloaterRequested>(OnDamage);
        }

        void Awake()
        {
            _pool = new TextMeshPro[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject($"DmgFloat_{i}");
                go.transform.SetParent(transform, false);
                var tmp = go.AddComponent<TextMeshPro>();
                tmp.fontSize = 5.2f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.gameObject.SetActive(false);
                _pool[i] = tmp;
            }
        }

        void OnDamage(LocalGameplayEvents.DamageFloaterRequested e)
        {
            var tmp = _pool[_idx];
            _idx = (_idx + 1) % poolSize;

            tmp.gameObject.SetActive(true);
            tmp.transform.position = e.WorldPosition;
            tmp.transform.rotation = Camera.main != null
                ? Quaternion.LookRotation(Camera.main.transform.forward)
                : Quaternion.identity;
            tmp.text = e.IsCritical ? $"<b>{e.Amount}!</b>" : e.Amount.ToString();
            tmp.color = e.IsPlayerSource ? new Color(1f, 0.85f, 0.35f) : Color.white;
            StartCoroutine(Animate(tmp));
        }

        IEnumerator Animate(TextMeshPro tmp)
        {
            float t = 0f;
            Vector3 p = tmp.transform.position;
            while (t < lifetime)
            {
                t += Time.deltaTime;
                tmp.transform.position = p + Vector3.up * (t * floatSpeed);
                var c = tmp.color;
                c.a = 1f - (t / lifetime);
                tmp.color = c;
                if (Camera.main != null)
                    tmp.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
                yield return null;
            }

            tmp.gameObject.SetActive(false);
        }
    }
}
