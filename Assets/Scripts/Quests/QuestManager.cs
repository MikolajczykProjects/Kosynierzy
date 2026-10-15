using System;
using UnityEngine;
using UnityEngine.Events;

namespace Kosynierzy
{
    /// <summary>
    /// Runs the linear story: activates the listed steps one after another.
    /// One per scene; the HUD and other systems read <see cref="Current"/> and the events.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        [SerializeField] QuestObjective[] steps;
        [SerializeField] bool startOnAwake = true;
        [Tooltip("Delay between finishing a step and starting the next one.")]
        [SerializeField] float nextStepDelay = 1.5f;
        [SerializeField] UnityEvent onAllCompleted = new();

        public static QuestManager Instance { get; private set; }

        public QuestObjective Current { get; private set; }
        public QuestObjective LastCompleted { get; private set; }
        public int CurrentIndex { get; private set; } = -1;
        public int StepCount => steps.Length;
        public bool AllCompleted { get; private set; }

        public event Action<QuestObjective> StepStarted;
        public event Action<QuestObjective> StepCompleted;
        public event Action AllStepsCompleted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (startOnAwake) StartStep(0);
        }

        public void StartStep(int index)
        {
            if (Current != null) Current.Completed -= OnStepCompleted;
            Current = null;
            CurrentIndex = index;

            if (index >= steps.Length)
            {
                AllCompleted = true;
                onAllCompleted.Invoke();
                AllStepsCompleted?.Invoke();
                Debug.Log("Kosynierzy: all quest steps completed.");
                return;
            }

            Current = steps[index];
            Current.Completed += OnStepCompleted;
            StepStarted?.Invoke(Current);
            Current.Begin();
        }

        void OnStepCompleted(QuestObjective step)
        {
            step.Completed -= OnStepCompleted;
            LastCompleted = step;
            if (step.CheckpointOnComplete)
            {
                var respawn = UnityEngine.Object.FindAnyObjectByType<PlayerRespawn>();
                if (respawn != null) respawn.SetCheckpoint(respawn.transform.position, respawn.transform.rotation);
            }
            StepCompleted?.Invoke(step);
            Current = null;
            Invoke(nameof(StartNext), nextStepDelay);
        }

        void StartNext() => StartStep(CurrentIndex + 1);
    }
}
