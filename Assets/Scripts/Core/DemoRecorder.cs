using System.Collections;
using System.IO;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Dev tool for README clips: grabs the game view (HUD included) as raw RGB frames, downscaled, while slowly turning
    /// the player's view. tools/gif/make_gif.js turns a frame folder into an animated GIF. Start from the editor:
    /// DemoRecorder.Record("Recordings/demo/01_dworzec", 32, 8, 12f).
    /// </summary>
    public class DemoRecorder : MonoBehaviour
    {
        public static bool Busy { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Busy = false;

        /// <param name="folder">Project-relative output folder (frames f000.rgb…, plus info.txt).</param>
        /// <param name="yawPerSecond">Degrees per second the player's view turns while recording.</param>
        public static void Record(string folder, int frames, float fps, float yawPerSecond, int width = 480, int height = 270)
        {
            if (Busy) return;
            var go = new GameObject("[DemoRecorder]");
            go.AddComponent<DemoRecorder>().StartCoroutine(go.GetComponent<DemoRecorder>().Run(folder, frames, fps, yawPerSecond, width, height));
        }

        IEnumerator Run(string folder, int frames, float fps, float yawPerSecond, int width, int height)
        {
            Busy = true;
            Directory.CreateDirectory(folder);
            var full = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            var small = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            var player = FindAnyObjectByType<FirstPersonController>();
            var rgb = new byte[width * height * 3];
            float step = 1f / fps;

            for (int i = 0; i < frames; i++)
            {
                float until = Time.unscaledTime + step;
                while (Time.unscaledTime < until)
                {
                    if (player != null) player.transform.Rotate(0f, yawPerSecond * Time.unscaledDeltaTime, 0f, Space.World);
                    yield return null;
                }
                yield return new WaitForEndOfFrame();
                if (full.width != Screen.width || full.height != Screen.height)
                {
                    full.Release();
                    full = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
                }
                ScreenCapture.CaptureScreenshotIntoRenderTexture(full);
                // The capture is upside down on most graphics APIs; flip while downscaling.
                Graphics.Blit(full, small, new Vector2(1f, -1f), new Vector2(0f, 1f));
                var prev = RenderTexture.active;
                RenderTexture.active = small;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                RenderTexture.active = prev;
                var raw = tex.GetRawTextureData();
                // ReadPixels rows are bottom-up; write top-down for the GIF tool.
                for (int y = 0; y < height; y++)
                    System.Buffer.BlockCopy(raw, (height - 1 - y) * width * 3, rgb, y * width * 3, width * 3);
                File.WriteAllBytes(Path.Combine(folder, $"f{i:000}.rgb"), rgb);
            }

            File.WriteAllText(Path.Combine(folder, "info.txt"), $"{width} {height} {frames} {fps}");
            full.Release(); small.Release();
            Destroy(tex);
            Busy = false;
            Destroy(gameObject);
        }
    }
}
