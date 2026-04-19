using System.Collections.Generic;
using UnityEngine;

namespace HackKU.Core
{
    // Drops physical BillPaper prefabs through a mail-slot spawn point on a randomized cadence.
    // Replaces the old time-based auto-bill system in FinanceScheduler.
    public class BillSpawner : MonoBehaviour
    {
        [System.Serializable]
        public struct BillTemplate
        {
            public string label;
            public float amount;
            [Tooltip("Weighted chance of being picked each spawn.")]
            public float weight;
        }

        [SerializeField] BillPaper billPrefab;
        [SerializeField] Transform spawnPoint;

        [Header("Cadence (real seconds)")]
        [SerializeField] float firstSpawnDelay = 5f;
        [SerializeField] float minGap = 8f;
        [SerializeField] float maxGap = 15f;

        [Tooltip("Max physical bills that can be sitting unpaid at once. Above this, spawner waits.")]
        [SerializeField] int maxConcurrent = 4;

        [SerializeField] BillTemplate[] templates = new[]
        {
            new BillTemplate { label = "Rent",      amount = 950f, weight = 1.2f },
            new BillTemplate { label = "Utilities", amount = 180f, weight = 2f },
            new BillTemplate { label = "Internet",  amount = 80f,  weight = 2f },
            new BillTemplate { label = "Credit Card", amount = 220f, weight = 1.5f },
            new BillTemplate { label = "Streaming", amount = 15f,  weight = 2f },
            new BillTemplate { label = "Phone Bill", amount = 60f,  weight = 2f },
        };

        float _nextSpawn;
        readonly List<BillPaper> _live = new List<BillPaper>();

        bool _armed;

        void OnEnable()
        {
            _armed = false;
            _nextSpawn = float.MaxValue; // wait for character pick before scheduling anything
        }

        void Update()
        {
            if (billPrefab == null || spawnPoint == null) return;
            _live.RemoveAll(b => b == null);
            // Hold fire until the player has picked a character (StatsManager.ActiveProfile set).
            if (!_armed)
            {
                var sm = StatsManager.Instance;
                if (sm == null || sm.ActiveProfile == null) return;
                _armed = true;
                _nextSpawn = Time.time + firstSpawnDelay;
            }
            if (Time.time < _nextSpawn) return;
            if (_live.Count >= maxConcurrent) { _nextSpawn = Time.time + 3f; return; }

            Spawn();
            _nextSpawn = Time.time + Random.Range(minGap, maxGap);
        }

        void Spawn()
        {
            var tpl = PickWeighted();
            if (string.IsNullOrEmpty(tpl.label)) return;

            Vector3 jitter = new Vector3(Random.Range(-0.15f, 0.15f), 0f, Random.Range(-0.15f, 0.15f));
            var inst = Instantiate(billPrefab, spawnPoint.position + jitter, spawnPoint.rotation);
            inst.label = tpl.label;
            inst.baseAmount = tpl.amount;
            _live.Add(inst);
            ToastHUD.Show("New bill", tpl.label + " — $" + Mathf.Round(tpl.amount), ToastKind.Info);
        }

        BillTemplate PickWeighted()
        {
            if (templates == null || templates.Length == 0) return default;
            float total = 0f;
            for (int i = 0; i < templates.Length; i++) total += Mathf.Max(0f, templates[i].weight);
            if (total <= 0f) return templates[0];
            float r = Random.value * total;
            float acc = 0f;
            for (int i = 0; i < templates.Length; i++)
            {
                acc += Mathf.Max(0f, templates[i].weight);
                if (r <= acc) return templates[i];
            }
            return templates[templates.Length - 1];
        }
    }
}
