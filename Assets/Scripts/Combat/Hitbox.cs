using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Optional damage zone on a collider (head, limbs). Projectiles multiply damage by
    /// <see cref="multiplier"/> and report headshots. The Health lives on a parent.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Hitbox : MonoBehaviour
    {
        [SerializeField] float multiplier = 2f;
        [SerializeField] bool isHead = true;

        public float Multiplier => multiplier;
        public bool IsHead => isHead;
    }
}
