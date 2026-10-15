using UnityEngine;

namespace Kosynierzy
{
    /// <summary>Mode picked in the main menu ("Nowa gra" / "Tryb historyczny"); gameplay reads it after the level loads.</summary>
    public static class GameMode
    {
        /// <summary>True when the player started with "Tryb historyczny".</summary>
        public static bool Historical;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Historical = false;
    }
}
