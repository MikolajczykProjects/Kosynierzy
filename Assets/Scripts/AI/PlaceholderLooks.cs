using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Readable primitive looks for placeholder characters (used only while the art prefab is missing):
    /// SD = field-grey-green, dark helmet, rifle on the back; Schupo = dark green-blue with a tall shako;
    /// civilians = browns / beiges / greys with a flat cap (men) or a hat and skirt (women, CH_*_K*);
    /// station workers = white-grey coats with raised arms (the "ArmsUp" child, hidden when freed); allies = dark brown.
    /// Parts have no colliders and are coloured by PlaceholderTint.
    /// </summary>
    public static class PlaceholderLooks
    {
        static readonly Color SdBody = Hex(0x5E6650);
        static readonly Color SdHelmet = Hex(0x3A3F33);
        static readonly Color RifleWood = Hex(0x3B2A1C);
        static readonly Color Leather = Hex(0x2B2218);
        static readonly Color PoliceBody = Hex(0x2F4A44);
        static readonly Color Shako = Hex(0x18221F);
        static readonly Color Brass = Hex(0xB08D3C);
        static readonly Color WorkerCoat = Hex(0xC9C9C2);
        static readonly Color AllyBody = Hex(0x4A3626);
        static readonly Color Skin = Hex(0xD7B08A);
        // Civilian coats keep CIE76 dE >= 17 from the SD body (0x5E6650) and >= 18 from the Schupo body (0x2F4A44)
        // so they read apart at 20 m before the helmet / shako shapes do (the old grey 0x6E6A62 and dark brown 0x5F5548
        // were only dE 10.3 / 10.8 from the SD).
        static readonly Color[] MenCoats = { Hex(0x7A5C3E), Hex(0x4A5878), Hex(0x6B4B3C), Hex(0x8B7355), Hex(0x4D4156) };
        static readonly Color[] WomenCoats = { Hex(0x9C8466), Hex(0xA39272), Hex(0x7D4E45), Hex(0x55606E), Hex(0x9B7A78) };
        static readonly Color[] Hats = { Hex(0x2E2A26), Hex(0x3D352C), Hex(0x4A4A48), Hex(0x5A4632) };

        /// <summary>Body colour of a placeholder of this type (also used for the test-setup materials).</summary>
        public static Color BodyColor(CharacterType type) => type switch
        {
            CharacterType.Enemy => SdBody,
            CharacterType.Police => PoliceBody,
            CharacterType.Civilian => MenCoats[0],
            CharacterType.Worker => WorkerCoat,
            _ => AllyBody,
        };

        /// <summary>Adds the parts to a normalized placeholder (feet pivot, "Body" capsule child).</summary>
        public static void Build(GameObject go, CharacterType type)
        {
            var root = go.transform;
            bool woman = go.name.Contains("_K");
            Color body = type == CharacterType.Civilian
                ? Pick(woman ? WomenCoats : MenCoats)
                : BodyColor(type);

            var bodyPart = root.Find("Body");
            if (bodyPart != null) Tint(bodyPart.gameObject, body);

            // Face plate: shows which way the placeholder looks.
            Part(root, PrimitiveType.Cube, "Face", new Vector3(0f, 1.56f, 0.24f), new Vector3(0.3f, 0.2f, 0.12f), Vector3.zero, Skin);

            switch (type)
            {
                case CharacterType.Enemy:
                    Part(root, PrimitiveType.Sphere, "Helmet", new Vector3(0f, 1.74f, 0f), new Vector3(0.48f, 0.3f, 0.5f), Vector3.zero, SdHelmet);
                    Part(root, PrimitiveType.Cylinder, "HelmetRim", new Vector3(0f, 1.66f, -0.02f), new Vector3(0.54f, 0.015f, 0.56f), Vector3.zero, SdHelmet);
                    Part(root, PrimitiveType.Cube, "Rifle", new Vector3(0.2f, 1.15f, -0.3f), new Vector3(0.06f, 1.15f, 0.07f), new Vector3(0f, 0f, -18f), RifleWood);
                    Part(root, PrimitiveType.Cylinder, "Belt", new Vector3(0f, 0.95f, 0f), new Vector3(0.73f, 0.04f, 0.73f), Vector3.zero, Leather);
                    break;

                case CharacterType.Police:
                    Part(root, PrimitiveType.Cylinder, "Shako", new Vector3(0f, 1.92f, 0f), new Vector3(0.34f, 0.16f, 0.34f), Vector3.zero, Shako);
                    Part(root, PrimitiveType.Cube, "Badge", new Vector3(0f, 1.95f, 0.17f), new Vector3(0.09f, 0.09f, 0.02f), Vector3.zero, Brass);
                    Part(root, PrimitiveType.Cylinder, "Belt", new Vector3(0f, 0.95f, 0f), new Vector3(0.73f, 0.04f, 0.73f), Vector3.zero, Hex(0x111111));
                    break;

                case CharacterType.Civilian:
                    if (woman)
                    {
                        Part(root, PrimitiveType.Cylinder, "Skirt", new Vector3(0f, 0.42f, 0f), new Vector3(0.82f, 0.42f, 0.82f), Vector3.zero, Shade(body, 0.8f));
                        Part(root, PrimitiveType.Sphere, "Hat", new Vector3(0f, 1.8f, 0f), new Vector3(0.36f, 0.16f, 0.36f), Vector3.zero, Pick(Hats));
                        Part(root, PrimitiveType.Cylinder, "HatBrim", new Vector3(0f, 1.77f, 0f), new Vector3(0.52f, 0.01f, 0.52f), Vector3.zero, Pick(Hats));
                    }
                    else
                    {
                        Color cap = Pick(Hats);
                        Part(root, PrimitiveType.Cylinder, "Cap", new Vector3(0f, 1.81f, 0.02f), new Vector3(0.4f, 0.035f, 0.42f), Vector3.zero, cap);
                        Part(root, PrimitiveType.Cube, "CapPeak", new Vector3(0f, 1.79f, 0.22f), new Vector3(0.3f, 0.02f, 0.14f), Vector3.zero, cap);
                    }
                    break;

                case CharacterType.Worker:
                    Part(root, PrimitiveType.Cylinder, "Trousers", new Vector3(0f, 0.3f, 0f), new Vector3(0.6f, 0.3f, 0.6f), Vector3.zero, Hex(0x3A3A3A));
                    var arms = new GameObject("ArmsUp").transform;
                    arms.SetParent(root, false);
                    Part(arms, PrimitiveType.Cube, "ArmL", new Vector3(-0.3f, 1.85f, 0f), new Vector3(0.09f, 0.55f, 0.09f), new Vector3(0f, 0f, -10f), WorkerCoat);
                    Part(arms, PrimitiveType.Cube, "ArmR", new Vector3(0.3f, 1.85f, 0f), new Vector3(0.09f, 0.55f, 0.09f), new Vector3(0f, 0f, 10f), WorkerCoat);
                    break;

                default:
                    Part(root, PrimitiveType.Cylinder, "Hat", new Vector3(0f, 1.83f, 0f), new Vector3(0.38f, 0.07f, 0.38f), Vector3.zero, Hex(0x231A12));
                    Part(root, PrimitiveType.Cylinder, "HatBrim", new Vector3(0f, 1.77f, 0f), new Vector3(0.54f, 0.01f, 0.54f), Vector3.zero, Hex(0x231A12));
                    break;
            }
        }

        static void Part(Transform parent, PrimitiveType shape, string partName, Vector3 localPosition, Vector3 localScale, Vector3 euler, Color color)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = partName;
            var col = go.GetComponent<Collider>();
            if (Application.isPlaying) Object.Destroy(col);
            else Object.DestroyImmediate(col);
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = localScale;
            Tint(go, color);
        }

        static void Tint(GameObject go, Color color)
        {
            var tint = go.GetComponent<PlaceholderTint>();
            if (tint == null) tint = go.AddComponent<PlaceholderTint>();
            tint.Color = color;
        }

        static Color Pick(Color[] palette) => palette[Random.Range(0, palette.Length)];

        static Color Shade(Color c, float k) => new(c.r * k, c.g * k, c.b * k, 1f);

        static Color Hex(int rgb) => new(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
    }
}
