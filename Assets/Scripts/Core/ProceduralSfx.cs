using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Synthesised placeholder sounds so the game is audible before the sound team delivers clips:
    /// pistol crack, rifle shot, impacts, footsteps, reload clicks, camera shutter, quest chimes.
    /// Generated once (a few hundred ms of audio each); SfxBank uses them only when its slot is empty.
    /// </summary>
    public static class ProceduralSfx
    {
        const int Rate = 44100;
        static readonly Dictionary<SfxBank.Sfx, AudioClip[]> cache = new();
        static System.Random rng = new(1939);

        public static AudioClip Get(SfxBank.Sfx id)
        {
            if (!cache.TryGetValue(id, out var clips))
            {
                clips = Build(id);
                cache[id] = clips;
            }
            return clips == null || clips.Length == 0 ? null : clips[UnityEngine.Random.Range(0, clips.Length)];
        }

        static AudioClip[] Build(SfxBank.Sfx id) => id switch
        {
            SfxBank.Sfx.Shot => Variants(3, i => Gunshot($"vis_{i}", 0.35f, 2600f, 120f, 0.9f)),
            SfxBank.Sfx.EnemyShot => Variants(3, i => Gunshot($"rifle_{i}", 0.6f, 1700f, 80f, 1f)),
            SfxBank.Sfx.DryFire => One(Click("dry", 0.02f, 3200f, 0.6f)),
            SfxBank.Sfx.ReloadStart => One(Concat("reload_out", Click("a", 0.03f, 1800f, 0.7f), Silence(0.12f), Click("b", 0.04f, 1200f, 0.8f))),
            SfxBank.Sfx.ReloadEnd => One(Concat("reload_in", Click("c", 0.035f, 1500f, 0.9f), Silence(0.08f), Click("d", 0.03f, 2400f, 0.7f))),
            SfxBank.Sfx.Hit => One(Thud("hit", 0.12f, 180f, 0.8f)),
            SfxBank.Sfx.Headshot => One(Concat("head", Thud("h1", 0.08f, 220f, 0.8f), Tone("h2", 0.12f, 1400f, 0.25f))),
            SfxBank.Sfx.Kill => One(Concat("kill", Thud("k1", 0.18f, 90f, 1f), Tone("k2", 0.25f, 660f, 0.18f))),
            SfxBank.Sfx.MultiKill => One(Concat("multi", Thud("m1", 0.15f, 90f, 1f), Tone("m2", 0.15f, 660f, 0.2f), Tone("m3", 0.25f, 880f, 0.2f))),
            SfxBank.Sfx.PlayerHurt => One(Thud("hurt", 0.25f, 70f, 1f)),
            SfxBank.Sfx.LowHealthHeartbeat => One(Concat("heart", Thud("b1", 0.1f, 55f, 0.9f), Silence(0.12f), Thud("b2", 0.1f, 50f, 0.7f))),
            SfxBank.Sfx.Footstep => Variants(4, i => Step($"step_{i}", 0.09f, 0.35f)),
            SfxBank.Sfx.Jump => One(Step("jump", 0.08f, 0.3f)),
            SfxBank.Sfx.Land => One(Thud("land", 0.14f, 110f, 0.6f)),
            SfxBank.Sfx.Slide => One(Swish("slide", 0.45f, 0.35f)),
            SfxBank.Sfx.Pickup => One(Concat("shutter", Click("s1", 0.015f, 4200f, 0.7f), Silence(0.05f), Click("s2", 0.025f, 2600f, 0.6f))),
            SfxBank.Sfx.QuestStep => One(Concat("quest", Tone("q1", 0.12f, 523f, 0.22f), Tone("q2", 0.3f, 784f, 0.22f))),
            SfxBank.Sfx.QuestComplete => One(Concat("done", Tone("d1", 0.15f, 523f, 0.22f), Tone("d2", 0.15f, 659f, 0.22f), Tone("d3", 0.5f, 784f, 0.25f))),
            SfxBank.Sfx.BulletImpact => Variants(3, i => Click($"imp_{i}", 0.05f, 900f + i * 300f, 0.5f)),
            SfxBank.Sfx.BulletWhizz => One(Swish("whizz", 0.18f, 0.4f)),
            _ => null,
        };

        static AudioClip[] One(AudioClip c) => new[] { c };

        static AudioClip[] Variants(int n, Func<int, AudioClip> make)
        {
            var arr = new AudioClip[n];
            for (int i = 0; i < n; i++) arr[i] = make(i);
            return arr;
        }

        static float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);

        static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create("sfx_" + name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Sharp crack (filtered noise with a very fast attack) + low body thump + short room tail.
        static AudioClip Gunshot(string name, float length, float crackHz, float bodyHz, float gain)
        {
            int n = Mathf.RoundToInt(length * Rate);
            var d = new float[n];
            float lp = 0f, a = Mathf.Clamp01(crackHz / Rate * 6.28f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                lp += (Noise() - lp) * a;
                float crack = lp * Mathf.Exp(-t * 38f) * 1.6f;
                float body = Mathf.Sin(2f * Mathf.PI * bodyHz * t * (1f - t * 0.8f)) * Mathf.Exp(-t * 22f) * 0.9f;
                float tail = lp * Mathf.Exp(-t * 7f) * 0.25f;
                d[i] = Mathf.Clamp((crack + body + tail) * gain, -1f, 1f);
            }
            return Make(name, d);
        }

        static AudioClip Click(string name, float length, float hz, float gain)
        {
            int n = Mathf.RoundToInt(length * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                d[i] = (Mathf.Sin(2f * Mathf.PI * hz * t) * 0.6f + Noise() * 0.4f) * Mathf.Exp(-t * 160f) * gain;
            }
            return Make(name, d);
        }

        static AudioClip Thud(string name, float length, float hz, float gain)
        {
            int n = Mathf.RoundToInt(length * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                d[i] = (Mathf.Sin(2f * Mathf.PI * hz * t * (1f - t)) + Noise() * 0.15f) * Mathf.Exp(-t * 18f) * gain;
            }
            return Make(name, d);
        }

        static AudioClip Tone(string name, float length, float hz, float gain)
        {
            int n = Mathf.RoundToInt(length * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Min(1f, t * 200f) * Mathf.Exp(-t * 5f);
                d[i] = (Mathf.Sin(2f * Mathf.PI * hz * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * hz * t)) * env * gain;
            }
            return Make(name, d);
        }

        static AudioClip Step(string name, float length, float gain)
        {
            int n = Mathf.RoundToInt(length * Rate);
            var d = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                lp += (Noise() - lp) * 0.08f;
                d[i] = lp * Mathf.Exp(-t * 45f) * gain * 3f;
            }
            return Make(name, d);
        }

        static AudioClip Swish(string name, float length, float gain)
        {
            int n = Mathf.RoundToInt(length * Rate);
            var d = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float k = t / length;
                lp += (Noise() - lp) * Mathf.Lerp(0.05f, 0.3f, k);
                d[i] = lp * Mathf.Sin(Mathf.PI * k) * gain * 2f;
            }
            return Make(name, d);
        }

        static AudioClip Silence(float length) => Make("silence", new float[Mathf.RoundToInt(length * Rate)]);

        static AudioClip Concat(string name, params AudioClip[] parts)
        {
            int total = 0;
            foreach (var p in parts) total += p.samples;
            var d = new float[total];
            int off = 0;
            foreach (var p in parts)
            {
                var buf = new float[p.samples];
                p.GetData(buf, 0);
                Array.Copy(buf, 0, d, off, buf.Length);
                off += buf.Length;
            }
            return Make(name, d);
        }
    }
}
