using UnityEngine;

namespace Kosynierzy
{
    /// <summary>Done when the player walks into this object's trigger collider.</summary>
    [RequireComponent(typeof(Collider))]
    public class ReachLocationObjective : QuestObjective
    {
        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (IsActive && IsPlayer(other)) Complete();
        }

        // Covers the case where the player is already standing inside when the step starts.
        void OnTriggerStay(Collider other)
        {
            if (IsActive && IsPlayer(other)) Complete();
        }
    }
}
