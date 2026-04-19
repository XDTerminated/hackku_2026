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

        [Header("Forced collection")]
        [Tooltip("Seconds after reaching the final late tier before the bill auto-pays itself.")]
        public float autoPayDelayAfterFinalTier = 8f;

        [Tooltip("Extra penalty multiplier applied on top of the final tier when the bill auto-collects.")]
        public float forcedPenaltyMultiplier = 1.25f;

        public TMP_Text label3D;

        float _spawnTime;
        bool _committed;
        int _lastTier = -1;
        Vector3 _baseScale;

        void Awake()
        {
            _spawnTime = Time.time;
            _baseScale = transform.localScale;
        }

        void Update()
        {
            if (_committed) return;
            float amount = GetCurrentAmount();
            UpdateLabel(amount);
            AnimatePulse();
            MaybeFireTierSfx();
            MaybeForceCollect();
        }

        bool IsAtFinalTier()
        {
            float elapsed = Time.time - _spawnTime;
            if (elapsed < graceSeconds || lateFeeTiers == null || lateFeeTiers.Length == 0) return false;
            int tier = Mathf.FloorToInt((elapsed - graceSeconds) / Mathf.Max(0.01f, tierSeconds));
            return tier >= lateFeeTiers.Length - 1;
        }

        float ForcedCollectAt()
        {
            if (lateFeeTiers == null || lateFeeTiers.Length == 0) return float.MaxValue;
            return _spawnTime + graceSeconds + tierSeconds * (lateFeeTiers.Length - 1) + Mathf.Max(0f, autoPayDelayAfterFinalTier);
        }

        void MaybeForceCollect()
        {
            if (_committed) return;
            if (Time.time < ForcedCollectAt()) return;
            ForceCollect();
        }

        public void ForceCollect()
        {
            if (_committed) return;
            _committed = true;
            float amount = GetCurrentAmount() * Mathf.Max(1f, forcedPenaltyMultiplier);
            var sm = StatsManager.Instance;
            if (sm != null)
            {
                sm.ApplyDelta(-amount, 0f, label + " (forced)");
                // Forced collection is genuinely bad — take a happiness hit too.
                sm.ApplyDelta(0f, -6f, "Missed bill");
            }
            ToastHUD.Show("-$" + Mathf.Round(amount), label + " — FORCE COLLECTED", ToastKind.Bill);
            HackKU.Core.SfxHub.Instance.PlayAt("bill_late", transform.position, 1.0f);
            HackKU.Core.SfxHub.Instance.PlayAt("bill_late", transform.position, 0.8f);
            Destroy(gameObject, 0.05f);
        }

        void AnimatePulse()
        {
            float elapsed = Time.time - _spawnTime;
            if (!IsOverdue())
            {
                if (transform.localScale != _baseScale) transform.localScale = _baseScale;
                return;
            }
            // Overdue — small pulsing scale that picks up with each tier.
            float overdueSec = elapsed - graceSeconds;
            int tier = Mathf.Clamp(Mathf.FloorToInt(overdueSec / Mathf.Max(0.01f, tierSeconds)), 0, lateFeeTiers.Length - 1);
            float freq = 2.2f + tier * 0.8f;
            float amp = 0.05f + tier * 0.015f;
            float s = 1f + Mathf.Sin(Time.time * Mathf.PI * 2f * freq) * amp;
            transform.localScale = _baseScale * s;
        }

        void MaybeFireTierSfx()
        {
            float elapsed = Time.time - _spawnTime;
            int tier = IsOverdue()
                ? Mathf.Clamp(Mathf.FloorToInt((elapsed - graceSeconds) / Mathf.Max(0.01f, tierSeconds)), 0, lateFeeTiers.Length - 1)
                : -1;
            if (tier != _lastTier)
            {
                if (tier >= 0 && _lastTier >= -1) // skip the very first neutral→-1 setup
                {
                    HackKU.Core.SfxHub.Instance.PlayAt("bill_late", transform.position, 0.7f);
                }
                _lastTier = tier;
            }
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
            string amountColor;
            string prefix = "";
            if (!IsOverdue())
            {
                float remaining = Mathf.Max(0f, graceSeconds - elapsed);
                bool soon = remaining <= 8f;
                string c = soon ? "#ff9933" : "#33cc66";
                status = $"<color={c}>{(soon ? "! " : "")}due in {Mathf.CeilToInt(remaining)}s</color>";
                amountColor = soon ? "#ffcc66" : "#ffffff";
            }
            else
            {
                int overdue = Mathf.CeilToInt(elapsed - graceSeconds);
                int tier = Mathf.Clamp(Mathf.FloorToInt((elapsed - graceSeconds) / Mathf.Max(0.01f, tierSeconds)), 0, lateFeeTiers.Length - 1);
                float mult = lateFeeTiers[tier];
                bool atFinal = IsAtFinalTier();
                if (atFinal)
                {
                    float secsToForce = Mathf.Max(0f, ForcedCollectAt() - Time.time);
                    status = $"<color=#ff2222><b>FORCED PAYMENT in {Mathf.CeilToInt(secsToForce)}s</b></color>";
                }
                else
                {
                    status = $"<color=#ff3333><b>LATE {overdue}s x{mult:0.##}</b></color>";
                }
                amountColor = "#ff6666";
                prefix = "<color=#ff3333><b>!!! </b></color>";
            }
            label3D.text = $"{prefix}<size=70%><b>{label}</b></size>\n<color={amountColor}><b>${Mathf.Round(amount)}</b></color>\n<size=55%>{status}</size>";
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
