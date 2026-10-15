using UnityEngine;

namespace Kosynierzy
{
    /// <summary>Done when the given Interactable (door, radio, NPC…) is used.</summary>
    public class InteractObjective : QuestObjective
    {
        [SerializeField] Interactable target;

        public Interactable Target => target;

        protected override void OnBegin()
        {
            if (target == null) { Debug.LogWarning($"Kosynierzy: '{name}' has no Interactable target.", this); return; }
            target.Interacted += OnInteracted;
        }

        protected override void OnEnd()
        {
            if (target != null) target.Interacted -= OnInteracted;
        }

        void OnInteracted(Interactable _) => Complete();
    }
}
