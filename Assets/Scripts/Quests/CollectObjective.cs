using UnityEngine;

namespace Kosynierzy
{
    /// <summary>Done when every listed Interactable (pickups) has been used.</summary>
    public class CollectObjective : QuestObjective
    {
        [SerializeField] Interactable[] items = new Interactable[0];

        int collected;

        public override string ProgressText => $"{collected} / {items.Length}";

        protected override void OnBegin()
        {
            collected = 0;
            foreach (var item in items) if (item != null) item.Interacted += OnCollected;
            if (items.Length == 0) Complete();
        }

        protected override void OnEnd()
        {
            foreach (var item in items) if (item != null) item.Interacted -= OnCollected;
        }

        void OnCollected(Interactable item)
        {
            item.Interacted -= OnCollected;
            if (++collected >= items.Length) Complete();
        }
    }
}
