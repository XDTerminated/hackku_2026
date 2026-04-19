using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace HackKU.Core
{
    // Unity TMP's default font (LiberationSans) has no emoji glyphs, so Unicode
    // emoji render as tofu boxes by default. This bootstrap creates a DYNAMIC font
    // asset from the OS's emoji font ("Segoe UI Emoji" on Windows, "Apple Color Emoji"
    // on macOS) at runtime and registers it as a global TMP fallback — after that,
    // any TMP text in the scene can contain real emoji.
    //
    // The OS font scan + dynamic font asset creation takes 50–200ms on Windows, which
    // used to block the pre-first-frame bootstrap. We now defer to after first frame so
    // Play-mode entry isn't stalled. Cost: happiness emoji renders as tofu for one frame.
    public class EmojiFontBootstrap : MonoBehaviour
    {
        [Tooltip("OS fonts to try, in order, for emoji glyph fallback.")]
        public string[] candidateOsFonts = new[]
        {
            "Segoe UI Emoji", "Apple Color Emoji", "Noto Color Emoji",
            "Symbola", "Segoe UI Symbol",
        };

        static bool _installed;

        IEnumerator Start()
        {
            yield return null; // let the first frame render before we block on font I/O
            TryInstallEmojiFallback();
        }

        public static void TryInstallEmojiFallback()
        {
            if (_installed) return;
            _installed = true;

            var osNames = new List<string>
            {
                "Segoe UI Emoji", "Apple Color Emoji", "Noto Color Emoji",
                "Symbola", "Segoe UI Symbol",
            };

            foreach (var name in osNames)
            {
                try
                {
                    var f = Font.CreateDynamicFontFromOSFont(name, 64);
                    if (f == null) continue;
                    var fa = TMP_FontAsset.CreateFontAsset(
                        f, 64, 4, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
                    if (fa == null) continue;
                    fa.name = "EmojiFallback_" + name;
                    if (TMP_Settings.fallbackFontAssets == null) continue;
                    if (!TMP_Settings.fallbackFontAssets.Contains(fa))
                        TMP_Settings.fallbackFontAssets.Add(fa);
                    Debug.Log("[EmojiFontBootstrap] registered fallback: " + name);
                    return;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("[EmojiFontBootstrap] skipped " + name + ": " + ex.Message);
                }
            }
            Debug.LogWarning("[EmojiFontBootstrap] no emoji font found; emoji will render as tofu.");
        }
    }
}
