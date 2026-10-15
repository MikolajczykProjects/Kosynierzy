using UnityEngine;

namespace Kosynierzy
{
    /// <summary>Subtitle bar at the bottom: speaker name in its colour, Kruk's thoughts in italics.</summary>
    public class DialogUI : MonoBehaviour
    {
        GUIStyle nameStyle, textStyle;

        static Color SpeakerColor(string who) => who switch
        {
            "kruk" => new Color(0.75f, 0.85f, 1f),
            "zofia" => new Color(1f, 0.85f, 0.55f),
            "naujocks" => new Color(1f, 0.45f, 0.4f),
            "heinz" => new Color(1f, 0.6f, 0.5f),
            "gruber" => new Color(0.7f, 1f, 0.7f),
            "sowa" => new Color(0.85f, 0.75f, 1f),
            "honiok" => new Color(0.9f, 0.9f, 0.9f),
            _ => new Color(0.95f, 0.95f, 0.85f),
        };

        static string DisplayName(string who) => string.IsNullOrEmpty(who) ? "" : who switch
        {
            "kruk" => "Kruk",
            "zofia" => "Pani Zofia",
            "sd" => "SD",
            "schupo" => "Schupo",
            _ => char.ToUpperInvariant(who[0]) + who.Substring(1),
        };

        void OnGUI()
        {
            var line = Dialog.CurrentLine;
            if (line == null) return;
            float scale = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale, h = 1080f;

            nameStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
            textStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 30, alignment = TextAnchor.UpperCenter, wordWrap = true };

            var box = new Rect(w * 0.5f - 600f, h - 230f, 1200f, 170f);
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = Color.white;

            string text = string.IsNullOrEmpty(line.Subtitle) ? line.Text : line.Subtitle;
            if (line.Thought)
            {
                textStyle.fontStyle = FontStyle.Italic;
                textStyle.normal.textColor = new Color(0.8f, 0.85f, 0.95f);
            }
            else
            {
                textStyle.fontStyle = FontStyle.Normal;
                textStyle.normal.textColor = Color.white;
                nameStyle.normal.textColor = SpeakerColor(line.Who);
                GUI.Label(new Rect(box.x, box.y + 14f, box.width, 36f), DisplayName(line.Who), nameStyle);
            }
            GUI.Label(new Rect(box.x + 40f, box.y + (line.Thought ? 30f : 54f), box.width - 80f, 110f), text, textStyle);
        }
    }
}
