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

        /// <summary>World point the camera checks (centre of renderers, else the transform).</summary>
        public Vector3 FocusPoint
        {
            get
            {
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
