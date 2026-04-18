using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HackKU.Core
{
    public class OutgoingCallMenu : MonoBehaviour
    {
        [Serializable]
        public class CallTarget
        {
            public string displayName;
            public DeliveryItem item;
        }

        public List<CallTarget> targets = new List<CallTarget>();
        public Canvas menuCanvas;
        public Transform menuAnchor;

        RotaryPhone phone;
        readonly List<GameObject> buttonInstances = new List<GameObject>();

        public void SetPhone(RotaryPhone p)
        {
            if (phone != null)
            {
                phone.OnDialOutRequested -= Show;
                phone.OnHungUp -= Hide;
            }
            phone = p;
            if (phone != null)
            {
                phone.OnDialOutRequested += Show;
                phone.OnHungUp += Hide;
            }
        }

        void OnDisable()
        {
            if (phone != null)
            {
                phone.OnDialOutRequested -= Show;
                phone.OnHungUp -= Hide;
            }
        }

        public void Show()
        {
            EnsureMenu();
            if (menuCanvas != null) menuCanvas.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (menuCanvas != null) menuCanvas.gameObject.SetActive(false);
        }

        void EnsureMenu()
        {
            if (menuCanvas == null) return;

            Transform listParent = menuAnchor != null ? menuAnchor : menuCanvas.transform;

            if (buttonInstances.Count == targets.Count) return;

            foreach (var go in buttonInstances) if (go != null) Destroy(go);
            buttonInstances.Clear();

            float y = -40f;
            foreach (var t in targets)
            {
                if (t == null || t.item == null) continue;
                var btnGo = new GameObject("Btn_" + t.displayName, typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(listParent, false);
                var rt = (RectTransform)btnGo.transform;
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(0, 60);
                rt.anchoredPosition = new Vector2(0, y);
                y -= 70f;

                var img = btnGo.GetComponent<Image>();
                img.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

                var txtGo = new GameObject("Label", typeof(RectTransform));
                txtGo.transform.SetParent(btnGo.transform, false);
                var txtRt = (RectTransform)txtGo.transform;
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = Vector2.zero;
                txtRt.offsetMax = Vector2.zero;
                var txt = txtGo.AddComponent<Text>();
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 22;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;
                txt.text = $"{t.displayName}  ${t.item.price:0}";

                var captured = t.item;
                btnGo.GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (DeliveryService.Instance != null) DeliveryService.Instance.DeliverItem(captured);
                    Hide();
                });

                buttonInstances.Add(btnGo);
            }
        }
    }
}
