using System.Collections.Generic;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Done when all targets are dead. With no targets listed, it takes every enemy that is
    /// alive in the scene when the step starts (handy while the level is still being built).
    /// </summary>
    public class KillObjective : QuestObjective
    {
        [Tooltip("Empty = all EnemyControllers alive when the step starts.")]
        [SerializeField] Health[] targets = new Health[0];

        readonly List<Health> alive = new();
        int total;

        public override string ProgressText => $"{total - alive.Count} / {total}";

        protected override void OnBegin()
        {
            alive.Clear();
            if (targets.Length > 0)
            {
                foreach (var h in targets) if (h != null && !h.IsDead) alive.Add(h);
            }
            else
            {
                foreach (var enemy in Object.FindObjectsByType<EnemyController>())
                {
                    var h = enemy.GetComponent<Health>();
                    if (h != null && !h.IsDead) alive.Add(h);
                }
            }
            total = alive.Count;
            foreach (var h in alive) h.Died += OnTargetDied;
            if (total == 0) Complete();
        }

        protected override void OnEnd()
        {
            foreach (var h in alive) if (h != null) h.Died -= OnTargetDied;
        }

        void OnTargetDied(Health h)
        {
            h.Died -= OnTargetDied;
            alive.Remove(h);
            if (alive.Count == 0) Complete();
        }
    }
}
