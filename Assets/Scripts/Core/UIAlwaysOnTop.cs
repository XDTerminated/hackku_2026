using UnityEngine;
using UnityEngine.UI;

namespace HackKU.Core
{
    // Forces every Graphic (Image / RawImage) and TMP text under the given root to
    // render on top of world geometry by switching to a ZTest-Always material.
    public static class UIAlwaysOnTop
    {
        static Material _imageMat;
        static Shader _overlayShader;

        public static void Apply(GameObject root)
        {
            if (root == null) return;
            EnsureImageMat();
            foreach (var g in root.GetComponentsInChildren<Graphic>(true))
            {
                if (g == null) continue;
                var tmp = g as TMPro.TextMeshProUGUI;
                if (tmp != null)
                {
                    // Instanced material clone so each TMP can set ZTest Always without
                    // polluting shared font asset.
                    var tmpMat = new Material(tmp.fontMaterial);
                    if (tmpMat.HasProperty("_ZTestMode")) tmpMat.SetFloat("_ZTestMode", 8f);
                    if (tmpMat.HasProperty("_ZTest")) tmpMat.SetFloat("_ZTest", 8f);
                    tmpMat.renderQueue = 4000;
                    tmp.fontMaterial = tmpMat;
                }
                else if (_imageMat != null)
                {
                    g.material = _imageMat;
                }
            }
        }

        static void EnsureImageMat()
        {
            if (_imageMat != null) return;
            if (_overlayShader == null) _overlayShader = Shader.Find("HackKU/UIOverlay");
            if (_overlayShader == null) return;
            _imageMat = new Material(_overlayShader);
            _imageMat.renderQueue = 4000;
        }
    }
}
