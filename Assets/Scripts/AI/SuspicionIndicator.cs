using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Floating "?" with the suspicion meter, or "!" in alarm / while reporting, above an SD (orange / red) or a Schupo
    /// (light blue / blue) head. Never on civilians or workers: it disables itself without a WatcherBrain.
    /// Drawn with IMGUI only when the head is in front of the camera, on screen and within Max Distance.
    /// </summary>
    [DisallowMultipleComponent]
    public class SuspicionIndicator : MonoBehaviour
    {
        static readonly Color SoldierQuestion = new(1f, 0.72f, 0.2f, 1f);
        static readonly Color SoldierAlarm = new(0.95f, 0.15f, 0.12f, 1f);
        static readonly Color PoliceQuestion = new(0.55f, 0.8f, 1f, 1f);
        static readonly Color PoliceAlarm = new(0.25f, 0.5f, 1f, 1f);

        [Tooltip("Height of the glyph above the feet.")]
        [SerializeField] float heightAboveFeet = 2.35f;
        [SerializeField] float maxDistance = 60f;

        static GUIStyle style;
        WatcherBrain brain;

        void Awake()
        {
            brain = GetComponent<WatcherBrain>();
            useGUILayout = false;
            var character = GetComponent<Character>();
            bool watcherType = character == null || character.Type == CharacterType.Enemy || character.Type == CharacterType.Police;
            if (brain == null || !watcherType) enabled = false;
        }

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (brain == null || !brain.isActiveAndEnabled || brain.IsDown) return;
            if (GameState.IsPaused || MissionFail.IsFailed) return;
            bool alarm = brain.IsAlerted;
            if (!alarm && !brain.ShowSuspicion) return;

            var cam = Camera.main != null ? Camera.main : PlayerTarget.Camera;
            if (cam == null) return;
            Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * heightAboveFeet);
            if (screen.z < 0.3f || screen.z > maxDistance) return;
            if (screen.x < 0f || screen.x > Screen.width || screen.y < 0f || screen.y > Screen.height) return;

            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                style.normal.textColor = Color.white;
            }

            bool police = brain.IsPolice;
            Color question = police ? PoliceQuestion : SoldierQuestion;
            Color alarmColor = police ? PoliceAlarm : SoldierAlarm;

            float scale = Mathf.Lerp(1f, 0.55f, screen.z / maxDistance) * (Screen.height / 1080f);
            float x = screen.x, y = Screen.height - screen.y;
            style.fontSize = Mathf.Max(10, Mathf.RoundToInt((alarm ? 64f : 54f) * scale));
            var rect = new Rect(x - 40f * scale, y - 52f * scale, 80f * scale, 52f * scale);
            string glyph = alarm ? "!" : "?";

            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), glyph, style);
            GUI.color = alarm ? alarmColor : question;
            GUI.Label(rect, glyph, style);

            if (!alarm)
            {
                float w = 46f * scale, h = Mathf.Max(3f, 6f * scale);
                var bar = new Rect(x - w * 0.5f, y + 2f * scale, w, h);
                GUI.color = new Color(0f, 0f, 0f, 0.6f);
                GUI.DrawTexture(bar, Texture2D.whiteTexture);
                float s = Mathf.Clamp01(brain.Suspicion);
                GUI.color = Color.Lerp(question, alarmColor, s * s);
                GUI.DrawTexture(new Rect(bar.x + 1f, bar.y + 1f, (bar.width - 2f) * s, bar.height - 2f), Texture2D.whiteTexture);
            }
            GUI.color = previous;
        }
    }
}
