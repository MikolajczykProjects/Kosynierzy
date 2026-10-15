using System;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Something worth photographing (map, list of uniforms, the SD car, Honiok's note…).
    /// The photo camera counts it when it is in frame, close enough and not hidden behind anything.
    /// Optional thought dialogue plays after the photo (e.g. "a1_mysl_mapa").
    /// </summary>
    public class Evidence : MonoBehaviour
    {
        [SerializeField] string id = "dowod";
        [SerializeField] string displayName = "Dowód";
        [SerializeField] string thoughtDialog;
        [Tooltip("Counts toward the X/6 evidence photos on the HUD.")]
        [SerializeField] bool countsAsEvidence = true;

        public string Id => id;
        public string DisplayName => displayName;
        public bool IsPhotographed { get; private set; }
        public event Action<Evidence> Photographed;

        public void Setup(string newId, string newName, string thought, bool counts = true)
        {
            id = newId;
            displayName = newName;
            thoughtDialog = thought;
            countsAsEvidence = counts;
        }

        Transform focusPoint;

        /// <summary>
        /// World point the camera checks: a child named "CelZdjecia" (team D's props — map table, list, note, microphone)
        /// when there is one, else the centre of the first solid collider (a car's body, not its door mesh), else the centre
        /// of the first renderer, else the transform.
        /// </summary>
        public Vector3 FocusPoint
        {
            get
            {
                if (focusPoint == null)
                {
                    foreach (var t in GetComponentsInChildren<Transform>(true))
                        if (t.name == "CelZdjecia") { focusPoint = t; break; }
                }
                if (focusPoint != null) return focusPoint.position;
                foreach (var c in GetComponentsInChildren<Collider>())
                    if (c.enabled && !c.isTrigger) return c.bounds.center;
                var r = GetComponentInChildren<Renderer>();
                return r != null ? r.bounds.center : transform.position;
            }
        }

        public void MarkPhotographed()
        {
            if (IsPhotographed) return;
            IsPhotographed = true;
            if (countsAsEvidence) GameProgress.AddPhoto(id);
            Photographed?.Invoke(this);
            if (!string.IsNullOrEmpty(thoughtDialog)) Dialog.Play(thoughtDialog, null, false);
        }
    }
}
