using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Real recordings for interactions: phone (a1_telefon starts), reception bell (a1_recepcja starts).
    /// Dialog has no "started" event, so the first line of each dialogue is detected by polling Dialog.CurrentLine.
    /// Cable-cut spark (sfx_przeciecie_kabla_iskra): no public event exists yet, so it is skipped.
    /// </summary>
    public class InteractionSfx : MonoBehaviour
    {
        static InteractionSfx instance;
        Dialog.Line lastLine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { instance = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (instance != null) return;
            var go = new GameObject("[InteractionSfx]");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<InteractionSfx>();
        }

        void Update()
        {
            var line = Dialog.CurrentLine;
            if (line == lastLine) return;
            lastLine = line;
            if (line == null) return;
            if (IsFirstLine("a1_telefon", line)) Play("Sfx/sfx_telefon_stacjonarny_1930");
            else if (IsFirstLine("a1_recepcja", line)) Play("Sfx/sfx_dzwonek_recepcja");
        }

        static bool IsFirstLine(string id, Dialog.Line line)
        {
            var lines = Dialog.GetLines(id);
            return lines != null && lines.Count > 0 && lines[0] == line;
        }

        static void Play(string path)
        {
            var clip = Resources.Load<AudioClip>(path);
            if (clip == null) return;
            var cam = Camera.main;
            var pos = cam != null ? cam.transform.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(clip, pos);
        }
    }
}
