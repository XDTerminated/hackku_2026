using TMPro;
using UnityEngine;

namespace HackKU.Core
{
    // Ticks an mm:ss elapsed timer on the wrist UI, counting real seconds since the
    // GameObject became enabled (i.e. since the run started).
    public class SessionTimerUI : MonoBehaviour
    {
        public TMP_Text elapsedText;
        [Tooltip("Session length in seconds. Once elapsed passes this, the label turns amber.")]
        public float targetSeconds = 300f;

        float _startTime;
        int _shownTotal = int.MinValue;
        bool _amberShown;

        static readonly Color AmberColor = new Color(1f, 0.55f, 0.3f);
        static readonly Color NormalColor = new Color(0.75f, 0.9f, 1f);

        void OnEnable() { _startTime = Time.time; _shownTotal = int.MinValue; _amberShown = false; }

        void Update()
        {
            if (elapsedText == null) return;
            float elapsed = Time.time - _startTime;
            int total = Mathf.Max(0, Mathf.FloorToInt(elapsed));
            if (total != _shownTotal)
            {
                _shownTotal = total;
                int mm = total / 60;
                int ss = total % 60;
                elapsedText.text = mm.ToString() + ":" + ss.ToString("00");
            }
            bool amber = elapsed >= targetSeconds;
            if (amber != _amberShown)
            {
                _amberShown = amber;
                elapsedText.color = amber ? AmberColor : NormalColor;
            }
        }
    }
}
