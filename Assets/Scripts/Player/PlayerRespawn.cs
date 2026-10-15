using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Team rule "one hit = end": when the player's Health dies, freeze controls and fail the mission
    /// ("Agent »Kruk« zdemaskowany…"). The failure screen offers the checkpoint reload.
    /// (Kept under its old name so existing scene references stay valid.)
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PlayerRespawn : MonoBehaviour
    {
        Health health;

        public bool IsRespawning => false;

        void Awake()
        {
            health = GetComponent<Health>();
            health.DestroyOnDeath = false; // the player must survive its own death: the camera/fail screen live on it
        }
        void OnEnable() => health.Died += OnDied;
        void OnDisable() => health.Died -= OnDied;

        /// <summary>Kept for old quest code; checkpoints are handled by Checkpoints now.</summary>
        public void SetCheckpoint(Vector3 position, Quaternion rotation) { }

        void OnDied(Health _)
        {
            var controller = GetComponent<FirstPersonController>();
            var weapon = GetComponentInChildren<PlayerWeapon>();
            if (controller) controller.enabled = false;
            if (weapon) weapon.enabled = false;
            CameraShake.AddTrauma(0.6f);
            MissionFail.Trigger(MissionFail.PlayerHit);
        }
    }
}
