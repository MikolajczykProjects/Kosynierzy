using System;
using UnityEngine;
using UnityEngine.Events;

namespace Kosynierzy
{
    /// <summary>
    /// Something the player can use with E: doors, crates, radios, NPCs to talk to.
    /// Needs a collider on this object or a child so the player's look ray can find it.
    /// Hook behaviour in the Inspector (onInteract) or from code (Interacted event).
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        [Tooltip("Shown as \"[E] <prompt>\".")]
        [SerializeField] string prompt = "Użyj";
        [SerializeField] bool singleUse;
        [Tooltip("Hide the object after use (pickups).")]
        [SerializeField] bool hideOnUse;
        [SerializeField] UnityEvent onInteract = new();

        public string Prompt => prompt;
        public bool CanInteract => enabled && !(singleUse && used);
        public event Action<Interactable> Interacted;

        bool used;

        public void SetPrompt(string text) => prompt = text;
        public void SetSingleUse(bool value) => singleUse = value;

        public void Interact()
        {
            if (!CanInteract) return;
            used = true;
            onInteract.Invoke();
            Interacted?.Invoke(this);
            if (hideOnUse) gameObject.SetActive(false);
        }
    }
}
