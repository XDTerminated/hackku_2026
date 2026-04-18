using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace HackKU.Core
{
    // A spawned physical bill. Picks up late-fee multiplier the longer it's ignored.
    // Grabbing it (XR grab) or touching it with the player triggers the charge and despawns.
    public class BillPaper : MonoBehaviour
    {
        public string label = "Bill";
        public float baseAmount = 200f;

        [Tooltip("Seconds before the first late-fee tier kicks in.")]
        public float graceSeconds = 25f;

        [Tooltip("Seconds between each escalating late-fee tier.")]
        public float tierSeconds = 20f;

        [Tooltip("Late-fee multipliers applied cumulatively after each tier elapses.")]
        public float[] lateFeeTiers = new[] { 1.25f, 1.5f, 2.0f, 3.0f };

        [Tooltip("Per-second amount added to the VISIBLE total while overdue, on top of tier multipliers. Does NOT touch the player's money until they interact.")]
        public float perSecondOverdueIncrement = 0f;

        public TMP_Text label3D;

        float _spawnTime;
        bool _committed;

        void Awake() { _spawnTime = Time.time; }

        void Update()
        {
            if (_committed) return;
            UpdateLabel(GetCurrentAmount());
        }

        public float GetCurrentAmount()
        {
            float elapsed = Time.time - _spawnTime;
            if (elapsed < graceSeconds) return baseAmount;
            float overdueSec = elapsed - graceSeconds;
            int tier = Mathf.Clamp(Mathf.FloorToInt(overdueSec / Mathf.Max(0.01f, tierSeconds)), 0, lateFeeTiers.Length - 1);
            float tiered = baseAmount * lateFeeTiers[tier];
            if (perSecondOverdueIncrement > 0f) tiered += perSecondOverdueIncrement * overdueSec;
            return tiered;
        }

        public bool IsOverdue() => Time.time - _spawnTime >= graceSeconds;

        void UpdateLabel(float amount)
        {
            if (label3D == null) return;
            float elapsed = Time.time - _spawnTime;
            string status;
            if (!IsOverdue())
            {
                float remaining = Mathf.Max(0f, graceSeconds - elapsed);
                status = $"<color=#228844>due in {Mathf.CeilToInt(remaining)}s</color>";
            }
            else
            {
                int overdue = Mathf.CeilToInt(elapsed - graceSeconds);
                int tier = Mathf.Clamp(Mathf.FloorToInt((elapsed - graceSeconds) / Mathf.Max(0.01f, tierSeconds)), 0, lateFeeTiers.Length - 1);
                float mult = lateFeeTiers[tier];
                status = $"<color=#cc2222>OVERDUE {overdue}s · {mult:0.##}x</color>";
            }
            label3D.text = $"<size=70%>{label}</size>\n<b>${Mathf.Round(amount)}</b>\n<size=55%>{status}</size>";
        }

        void OnEnable()
        {
            var grab = GetComponent<XRGrabInteractable>();
            if (grab != null) grab.selectEntered.AddListener(_ => Commit());
        }

        void OnTriggerEnter(Collider other)
        {
            if (_committed) return;
            if (other == null) return;
            if (other.CompareTag("Player") || other.CompareTag("MainCamera")) Commit();
            else if (other.GetComponentInParent<CharacterController>() != null) Commit();
        }

        public void Commit()
        {
            if (_committed) return;
            _committed = true;
            float amount = GetCurrentAmount();
            var sm = StatsManager.Instance;
            if (sm != null) sm.ApplyDelta(-amount, 0f, label);
            ToastHUD.Show("-$" + Mathf.Round(amount), label + (amount > baseAmount ? " (late)" : ""), ToastKind.Bill);
            Destroy(gameObject, 0.05f);
        }
    }
}
