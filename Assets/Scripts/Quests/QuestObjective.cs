using System;
using UnityEngine;
using UnityEngine.Events;

namespace Kosynierzy
{
    /// <summary>
    /// One step of the linear story. QuestManager activates steps one at a time in list order;
    /// a step only reacts to the world while it is active. Subclasses decide when it is done.
    /// Story/level designers add steps as components on scene objects and wire extra effects
    /// (open a gate, start a dialogue, spawn enemies) with onStarted / onCompleted.
    /// </summary>
    public abstract class QuestObjective : MonoBehaviour
    {
        [SerializeField] string title = "Nowe zadanie";
        [TextArea(2, 4)]
        [SerializeField] string description;
        [Tooltip("Where the HUD marker points. Empty = this object. Hidden when showMarker is off.")]
        [SerializeField] Transform marker;
        [SerializeField] bool showMarker = true;
        [Tooltip("Respawn the player here after this step is completed.")]
        [SerializeField] bool checkpointOnComplete = true;
        [SerializeField] UnityEvent onStarted = new();
        [SerializeField] UnityEvent onCompleted = new();

        public string Title => title;
        public string Description => description;
        public bool ShowMarker => showMarker;
        public Vector3 MarkerPosition => (marker != null ? marker : transform).position;
        public bool CheckpointOnComplete => checkpointOnComplete;
        public bool IsActive { get; private set; }
        public bool IsCompleted { get; private set; }

        /// <summary>Optional counter for the HUD, e.g. "2 / 5".</summary>
        public virtual string ProgressText => null;

        public event Action<QuestObjective> Completed;

        internal void Begin()
        {
            if (IsActive || IsCompleted) return;
            IsActive = true;
            onStarted.Invoke();
            OnBegin();
        }

        /// <summary>Finish the step; also callable from UnityEvents for scripted steps.</summary>
        public void Complete()
        {
            if (!IsActive || IsCompleted) return;
            IsActive = false;
            IsCompleted = true;
            OnEnd();
            onCompleted.Invoke();
            Completed?.Invoke(this);
        }

        protected virtual void OnBegin() { }
        protected virtual void OnEnd() { }

        protected static bool IsPlayer(Component c) => c.GetComponentInParent<FirstPersonController>() != null;
    }
}
