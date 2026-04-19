using UnityEngine;
using UnityEngine.SceneManagement;

namespace HackKU.Core
{
    // Ensures every scene load (first launch, restart button, etc.) starts from a
    // clean slate. Clears static run-state (owned furniture, happiness multipliers)
    // and resets any DontDestroyOnLoad singletons that carry per-run data.
    public static class NewRunReset
    {
        // Runs before anything else — including domain-reload-disabled Play mode entry.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearStaticsOnBoot()
        {
            GhostRegistry.ResetAll();
            HappinessMultiplierStack.ResetAll();
        }

        // Also fires on every scene load so a restart wipes the slate too.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void HookSceneLoad()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            ResetRunState();
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ResetRunState();
        }

        static void ResetRunState()
        {
            GhostRegistry.ResetAll();
            HappinessMultiplierStack.ResetAll();

            // Reset the stats tracker so the win screen shows THIS run's numbers, not accumulated.
            var tracker = Object.FindFirstObjectByType<RunStatsTracker>();
            if (tracker != null) tracker.ResetRun();

            // Nuke any leftover win-screen canvases from the prior run.
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go == null) continue;
                if (go.name == "[WinScreen]") Object.Destroy(go);
            }
        }
    }
}
