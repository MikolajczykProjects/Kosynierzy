using System.Collections.Generic;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Switches player input off while a full-screen UI (history card, fail screen, epilogue) is up, and back on
    /// afterwards. Only re-enables the components it switched off itself, so it never fights Dialog's freeze.
    /// </summary>
    public sealed class PlayerControlLock
    {
        readonly List<Behaviour> disabled = new();

        public bool IsLocked { get; private set; }

        public void Lock()
        {
            if (IsLocked) return;
            IsLocked = true;
            Disable(UnityEngine.Object.FindObjectsByType<FirstPersonController>());
            Disable(UnityEngine.Object.FindObjectsByType<PlayerWeapon>());
            // E closes the history card; without this the same press would also "use" whatever is in front of the player.
            Disable(UnityEngine.Object.FindObjectsByType<PlayerInteractor>());
        }

        public void Unlock()
        {
            if (!IsLocked) return;
            IsLocked = false;
            foreach (var b in disabled)
                if (b != null) b.enabled = true;
            disabled.Clear();
        }

        void Disable(Behaviour[] behaviours)
        {
            foreach (var b in behaviours)
            {
                if (b == null || !b.enabled) continue;
                b.enabled = false;
                disabled.Add(b);
            }
        }

        /// <summary>Cursor for clicking buttons (unlocked, visible) or for play (locked, hidden).</summary>
        public static void SetCursorFree(bool free)
        {
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = free;
        }
    }
}
