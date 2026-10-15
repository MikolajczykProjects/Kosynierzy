using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Very visible objective marker: a pulsing light pillar in the world at the current step's MarkerPosition
    /// plus a diamond with the distance in metres, clamped to the screen edge when the target is off-screen.
    /// Created at runtime by Mission (no scene setup). IMGUI overlay, so it does not touch the uGUI HUD files.
    /// </summary>
    public class ObjectiveBeacon : MonoBehaviour
    {
        static readonly Color Gold = new Color(1f, 0.82f, 0.25f, 1f);

        Transform pillar;
        Material pillarMat;
        Texture2D dot;
        Vector3? target;
        Camera cam;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<ObjectiveBeacon>() == null)
                new GameObject("ObjectiveBeacon").AddComponent<ObjectiveBeacon>();
        }

        void Awake()
        {
            dot = Texture2D.whiteTexture;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "BeaconPillar";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            pillar = go.transform;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            pillarMat = new Material(shader);
            pillarMat.SetFloat("_Surface", 1f);
            pillarMat.SetFloat("_Blend", 1f); // additive
            pillarMat.SetFloat("_ZWrite", 0f);
            pillarMat.renderQueue = 3100;
            pillarMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = pillarMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pillar.gameObject.SetActive(false);
        }

        void OnDestroy() { if (pillarMat != null) Destroy(pillarMat); }

        void Update()
        {
            var m = Mission.Instance;
            target = null;
            if (m != null && m.Current != null && !m.Finished && !MissionFail.IsFailed) target = m.Current.MarkerPosition;
            if (cam == null) cam = Camera.main;

            if (target == null) { pillar.gameObject.SetActive(false); return; }
            pillar.gameObject.SetActive(true);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
            // Pillar: 60 m tall, wider with distance so it reads from far away.
            float dist = cam != null ? Vector3.Distance(cam.transform.position, target.Value) : 20f;
            float width = Mathf.Clamp(0.35f + dist * 0.012f, 0.35f, 2.5f);
            pillar.position = target.Value + Vector3.up * 30f;
            pillar.localScale = new Vector3(width, 30f, width);
            var col = Gold; col.a = Mathf.Lerp(0.18f, 0.4f, pulse);
            if (pillarMat.HasProperty("_BaseColor")) pillarMat.SetColor("_BaseColor", col);
            if (pillarMat.HasProperty("_Color")) pillarMat.SetColor("_Color", col);
            if (dist < 6f) pillar.gameObject.SetActive(false); // standing on it: no pillar in the face
        }

        void OnGUI()
        {
            if (target == null || cam == null || GameState.IsPaused || Event.current.type != EventType.Repaint) return;
            Vector3 world = target.Value + Vector3.up * 2.5f;
            Vector3 sp = cam.WorldToScreenPoint(world);
            bool behind = sp.z < 0f;
            float dist = Vector3.Distance(cam.transform.position, target.Value);
            float W = Screen.width, H = Screen.height, pad = 46f;
            float x = sp.x, y = H - sp.y;
            if (behind) { x = W - x; y = H - y; }
            bool off = behind || x < pad || x > W - pad || y < pad || y > H - pad;
            if (off)
            {
                var c = new Vector2(W * 0.5f, H * 0.5f);
                var d = new Vector2(x, y) - c;
                if (d.sqrMagnitude < 1f) d = Vector2.up;
                float k = Mathf.Min((W * 0.5f - pad) / Mathf.Max(0.01f, Mathf.Abs(d.x)), (H * 0.5f - pad) / Mathf.Max(0.01f, Mathf.Abs(d.y)));
                x = c.x + d.x * k; y = c.y + d.y * k;
            }

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
            float s = 18f + 5f * pulse;
            var old = GUI.matrix; var oldCol = GUI.color;
            GUIUtility.RotateAroundPivot(45f, new Vector2(x, y));
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(x - s - 3f, y - s - 3f, 2f * s + 6f, 2f * s + 6f), dot);
            GUI.color = new Color(Gold.r, Gold.g, Gold.b, 0.95f);
            GUI.DrawTexture(new Rect(x - s, y - s, 2f * s, 2f * s), dot);
            GUI.matrix = old;

            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 18, fontStyle = FontStyle.Bold };
            string txt = dist >= 1000f ? $"{dist / 1000f:0.0} km" : $"{Mathf.RoundToInt(dist)} m";
            var r = new Rect(x - 60f, y + s + 6f, 120f, 26f);
            GUI.color = Color.black; GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), txt, style);
            GUI.color = Gold; GUI.Label(r, txt, style);
            GUI.color = oldCol;
        }
    }
}
