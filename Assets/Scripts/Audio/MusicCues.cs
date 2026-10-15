using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Story music cues: polls the current Mission step every 0.5 s and asks GameMusic to play the matching track.
    /// Never overrides alarm / searching / fail music (only re-applies the cue while GameMusic is on calm exploration).
    /// </summary>
    public sealed class MusicCues : MonoBehaviour
    {
        static MusicCues instance;
        MissionStep lastStep;
        string wanted = "";
        float nextPoll;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (instance != null) return;
            var go = new GameObject("MusicCues");
            instance = go.AddComponent<MusicCues>();
            DontDestroyOnLoad(go);
        }

        void Update()
        {
            if (Time.unscaledTime < nextPoll) return;
            nextPoll = Time.unscaledTime + 0.5f;

            var m = Mission.Instance;
            if (m == null) { lastStep = null; wanted = ""; return; }
            var step = m.Current;
            if (step == null) return;

            if (step != lastStep)
            {
                lastStep = step;
                string pick = Pick(step);
                if (pick != null) wanted = pick;
                else if (step is ReachStep || step is TalkStep) { /* keep previous cue */ }
            }

            if (wanted.Length == 0) return;
            var st = Detection.PlayerStatus;
            if (st == AwarenessLevel.Alarmed || st == AwarenessLevel.Searching) return;
            string cur = GameMusic.Current;
            if (cur == wanted) return;
            // only take over from calm exploration (or an earlier story cue), never from fail/action/tension
            if (cur == "mus_eksploracja_spokojna" || cur == "" || IsCue(cur))
                GameMusic.Play(wanted, 1.5f, 25f);
        }

        static bool IsCue(string id)
        {
            return id == "mus_sledzenie_auta" || id == "mus_zmrok_przejscie" || id == "mus_zagajnik_konserwa"
                || id == "mus_fotograf_ciemnia_noir" || id == "mus_3d_gramofon_kawiarnia"
                || id == "mus_kulminacja_lacznica" || id == "mus_ucieczka_pola" || id == "mus_ucieczka_rower";
        }

        static string Pick(MissionStep s)
        {
            string t = (s.Title ?? "").ToLowerInvariant();
            if (s is FollowCarStep) return "mus_sledzenie_auta";
            if (s is CableStep) return "mus_kulminacja_lacznica";
            if (t.Contains("fotograf") && !t.Contains("sfotograf")) return "mus_fotograf_ciemnia_noir";
            if (t.Contains("zagajnik") || t.Contains("konserw")) return "mus_zagajnik_konserwa";
            if (s is ReachAnyStep) return "mus_zmrok_przejscie";
            if (t.Contains("rower")) return "mus_ucieczka_rower";
            if (t.Contains("stodo") || t.Contains("uciekaj") || t.Contains("kartk")) return "mus_ucieczka_pola";
            if (t.Contains("hotel") || t.Contains("naujocks") || t.Contains("recepcj") || t.Contains("telefon") || t.Contains("mapę"))
                return "mus_3d_gramofon_kawiarnia";
            return null;
        }
    }
}
