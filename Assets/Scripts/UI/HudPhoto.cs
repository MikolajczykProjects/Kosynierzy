using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// 1930s rangefinder viewfinder for PhotoCamera: dark rounded mask, bright-line frame over the 0.2–0.8 area that
    /// counts for evidence, golden focus patch, "KLATKA nn" counter; the shutter flash; and after each shot a small
    /// paper print sliding in bottom-left with the caption and a red "DOWÓD" stamp when the photo counted.
    /// </summary>
    public sealed class HudPhoto : HudWidget
    {
        const float PrintIn = 0.35f, PrintHold = 4f, PrintOut = 0.35f;
        const float PrintX = 64f, PrintY = 150f;

        RectTransform viewfinder;
        UnityEngine.UI.Text frameCounter;
        UnityEngine.UI.Image flash;

        RectTransform printRoot;
        UnityEngine.UI.RawImage printImage;
        UnityEngine.UI.Text printCaption, printEmpty;
        RectTransform evidenceStamp;
        float printTime = -1f;

        PhotoCamera subscribed;
        int filmTotal;

        public override void Build(HudContext ctx)
        {
            var root = ctx.Root;

            viewfinder = SettingsMenuUi.CreateRect("Viewfinder", root);
            SettingsMenuUi.Stretch(viewfinder);
            var mask = SettingsMenuUi.CreateImage("Mask", viewfinder, UiTheme.WithAlpha(UiTheme.Dark, 0.97f), false);
            mask.sprite = UiTheme.ViewfinderMask;
            SettingsMenuUi.Stretch(mask.rectTransform);

            // Bright-line frame = the part of the picture that counts (0.2–0.8 of the screen).
            var frame = SettingsMenuUi.CreateRect("BrightLines", viewfinder);
            SettingsMenuUi.Anchor(frame, 0.2f, 0.2f, 0.8f, 0.8f);
            UiTheme.AddFrame(frame, UiTheme.WithAlpha(UiTheme.Bone, 0.5f), 1f, 0f);
            Corner(frame, new Vector2(0f, 0f), 1f, 1f);
            Corner(frame, new Vector2(1f, 0f), -1f, 1f);
            Corner(frame, new Vector2(0f, 1f), 1f, -1f);
            Corner(frame, new Vector2(1f, 1f), -1f, -1f);

            var patch = SettingsMenuUi.CreateImage("FocusPatch", viewfinder, UiTheme.WithAlpha(UiTheme.Warn, 0.14f), false);
            SettingsMenuUi.Place(patch.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70f, 46f));
            UiTheme.AddFrame(patch.rectTransform, UiTheme.WithAlpha(UiTheme.Warn, 0.85f), 1.5f, 0f);

            frameCounter = UiTheme.CreateHudText("Counter", viewfinder, "", 20, TextAnchor.MiddleRight, UiTheme.Bone);
            frameCounter.rectTransform.anchorMin = frameCounter.rectTransform.anchorMax = new Vector2(0.8f, 0.2f);
            frameCounter.rectTransform.pivot = new Vector2(1f, 1f);
            frameCounter.rectTransform.sizeDelta = new Vector2(240f, 30f);
            frameCounter.rectTransform.anchoredPosition = new Vector2(0f, -10f);
            viewfinder.gameObject.SetActive(false);

            // The print: a small paper photo in the corner.
            var paper = UiTheme.CreatePaper(root, "Print", new Vector2(300f, 236f), out printRoot, 3f);
            printRoot.anchorMin = printRoot.anchorMax = printRoot.pivot = Vector2.zero;
            printRoot.localRotation = Quaternion.Euler(0f, 0f, 2f);
            var photoBack = SettingsMenuUi.CreateImage("PhotoBack", paper, UiTheme.WithAlpha(UiTheme.Dark, 0.9f), false);
            SettingsMenuUi.PinTop(photoBack.rectTransform, 12f, 156f, 12f);
            // A GameObject holds one Graphic, so the photo is a RawImage child of the dark backing.
            var raw = SettingsMenuUi.CreateRect("Photo", photoBack.rectTransform);
            SettingsMenuUi.Stretch(raw, 4f, 4f, 4f, 4f);
            printImage = raw.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            printImage.raycastTarget = false;
            printImage.color = new Color(0.92f, 0.88f, 0.8f, 1f); // slight sepia
            printEmpty = SettingsMenuUi.CreateText("Empty", photoBack.rectTransform, "—", 30, TextAnchor.MiddleCenter, FontStyle.Bold,
                UiTheme.WithAlpha(UiTheme.Bone, 0.4f));
            SettingsMenuUi.Stretch(printEmpty.rectTransform);
            printCaption = SettingsMenuUi.CreateText("Caption", paper, "", 17, TextAnchor.UpperLeft, FontStyle.Normal, UiTheme.Ink);
            SettingsMenuUi.Stretch(printCaption.rectTransform, 14f, 14f, 174f, 6f);
            evidenceStamp = UiTheme.CreateStamp(paper, "Dowód", UiTheme.StampRed, 24, -9f);
            evidenceStamp.anchorMin = evidenceStamp.anchorMax = new Vector2(1f, 1f);
            evidenceStamp.anchoredPosition = new Vector2(-70f, -128f);
            printRoot.gameObject.SetActive(false);

            flash = SettingsMenuUi.CreateImage("Flash", root, UiTheme.WithAlpha(UiTheme.Bone, 0f), false);
            SettingsMenuUi.Stretch(flash.rectTransform);
        }

        static void Corner(RectTransform frame, Vector2 corner, float dirX, float dirY)
        {
            const float length = 28f, thick = 2f;
            var color = UiTheme.WithAlpha(UiTheme.Bone, 0.95f);
            var h = SettingsMenuUi.CreateImage("CornerH", frame, color, false);
            var hr = h.rectTransform;
            hr.anchorMin = hr.anchorMax = corner;
            hr.pivot = new Vector2(dirX > 0f ? 0f : 1f, dirY > 0f ? 0f : 1f);
            hr.sizeDelta = new Vector2(length, thick);
            hr.anchoredPosition = Vector2.zero;
            var v = SettingsMenuUi.CreateImage("CornerV", frame, color, false);
            var vr = v.rectTransform;
            vr.anchorMin = vr.anchorMax = corner;
            vr.pivot = hr.pivot;
            vr.sizeDelta = new Vector2(thick, length);
            vr.anchoredPosition = Vector2.zero;
        }

        public override void Dispose()
        {
            if (subscribed != null) subscribed.PhotoTaken -= OnPhotoTaken;
            subscribed = null;
        }

        void OnPhotoTaken(Texture2D photo, string caption, bool counted)
        {
            printImage.texture = photo;
            printImage.enabled = photo != null;
            printEmpty.enabled = photo == null;
            printCaption.text = string.IsNullOrEmpty(caption) ? "Nic ważnego w kadrze" : caption;
            evidenceStamp.gameObject.SetActive(counted);
            printTime = 0f;
            printRoot.gameObject.SetActive(true);
        }

        public override void Tick(HudContext ctx)
        {
            var photo = ctx.Photo;
            if (photo != subscribed)
            {
                if (subscribed != null) subscribed.PhotoTaken -= OnPhotoTaken;
                subscribed = photo;
                if (subscribed != null) subscribed.PhotoTaken += OnPhotoTaken;
            }

            viewfinder.gameObject.SetActive(ctx.ViewfinderUp);
            if (ctx.ViewfinderUp)
            {
                filmTotal = Mathf.Max(filmTotal, photo.ShotsLeft);
                frameCounter.text = photo.ShotsLeft <= 0 ? "Brak kadrów" : $"Zdjęcie {filmTotal - photo.ShotsLeft + 1} / {filmTotal}";
            }
            flash.color = UiTheme.WithAlpha(UiTheme.Bone, photo != null ? Mathf.Clamp01(photo.FlashAlpha) * 0.9f : 0f);

            if (printTime < 0f) return;
            printTime += ctx.Dt;
            float x;
            if (printTime < PrintIn) x = Mathf.Lerp(-340f, PrintX, 1f - Mathf.Pow(1f - printTime / PrintIn, 2f));
            else if (printTime < PrintIn + PrintHold) x = PrintX;
            else if (printTime < PrintIn + PrintHold + PrintOut) x = Mathf.Lerp(PrintX, -340f, (printTime - PrintIn - PrintHold) / PrintOut);
            else
            {
                printTime = -1f;
                printRoot.gameObject.SetActive(false);
                return;
            }
            printRoot.anchoredPosition = new Vector2(x, PrintY);
        }
    }
}
