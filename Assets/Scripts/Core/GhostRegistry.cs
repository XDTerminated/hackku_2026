using System;
using System.Collections.Generic;

namespace HackKU.Core
{
    public static class GhostRegistry
    {
        static readonly HashSet<string> _owned = new HashSet<string>();
        public static event Action<string> OnOwnedChanged;

        public static bool IsOwned(string id) =>
            !string.IsNullOrEmpty(id) && _owned.Contains(id);

        public static void MarkOwned(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (_owned.Add(id)) OnOwnedChanged?.Invoke(id);
        }

        public static void ResetAll()
        {
            _owned.Clear();
            OnOwnedChanged?.Invoke(null);
        }
    }
}
