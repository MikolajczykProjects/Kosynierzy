using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Cykl dobowy mapy Gliwic 1939. Jeden obiekt w scenie (na korzeniu Mapa_Gliwice).
/// 15:30 = dzień (słońce wysoko, ciepłe), 19:00 = zachód (pomarańcz, nisko),
/// od 20:00 = noc (światło księżycowe, niebieskie, ciemny ambient).
/// Latarnie (MapaLampa z noc=true) zapalają się od 19:30.
/// </summary>
public class MapaSwiatlo : MonoBehaviour
{
    public static MapaSwiatlo Instance;

    [Range(14f, 22f)]
    public float godzina = 15.5f;

    [Header("Przypisywane przez ZbudujGliwice")]
    public Light slonce;
    public List<Material> materialyEmisjaNocna = new List<Material>();

    const float GODZINA_ZACHOD = 19f;
    const float GODZINA_NOC = 20f;
    const float GODZINA_LAMPY = 19.5f;

    void Awake()
    {
        Instance = this;
        Ustaw(godzina);
    }

    void OnValidate()
    {
        // Pozwala przesuwać "godzina" w Inspectorze i widzieć zmianę natychmiast.
        if (!Application.isPlaying || Instance == this)
        {
            Instance = this;
            Ustaw(godzina);
        }
    }

    public static void Ustaw(float godzinaNowa)
    {
        if (Instance == null) return;
        Instance.godzina = godzinaNowa;
        Instance.Zastosuj();
    }

    void Zastosuj()
    {
        float t = Mathf.InverseLerp(14f, GODZINA_ZACHOD, godzina);     // 0 = pełny dzień, 1 = zachód
        float n = Mathf.InverseLerp(GODZINA_ZACHOD, GODZINA_NOC, godzina); // 0 = zachód, 1 = noc

        Color kolorDzien = new Color(1f, 0.96f, 0.88f);
        Color kolorZachod = new Color(1f, 0.55f, 0.25f);
        Color kolorNoc = new Color(0.45f, 0.55f, 0.85f);

        Color kolorSlonca;
        float wysokoscSlonca; // kąt nad horyzontem w stopniach
        float intensywnoscSlonca;

        if (godzina <= GODZINA_ZACHOD)
        {
            kolorSlonca = Color.Lerp(kolorDzien, kolorZachod, t);
            wysokoscSlonca = Mathf.Lerp(55f, 8f, t);
            intensywnoscSlonca = Mathf.Lerp(1.25f, 0.9f, t);
        }
        else
        {
            kolorSlonca = Color.Lerp(kolorZachod, kolorNoc, n);
            wysokoscSlonca = Mathf.Lerp(8f, -15f, n);
            intensywnoscSlonca = Mathf.Lerp(0.9f, 0.05f, n);
        }

        if (slonce != null)
        {
            slonce.color = kolorSlonca;
            slonce.intensity = intensywnoscSlonca;
            slonce.transform.rotation = Quaternion.Euler(wysokoscSlonca, 200f, 0f);
            slonce.shadows = LightShadows.Soft;
        }

        // Ambient trilight + mgła
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        Color skyDzien = new Color(0.55f, 0.65f, 0.85f);
        Color skyNoc = new Color(0.05f, 0.07f, 0.14f);
        Color groundDzien = new Color(0.35f, 0.3f, 0.22f);
        Color groundNoc = new Color(0.03f, 0.03f, 0.05f);
        float nocTotal = Mathf.Clamp01(n * (godzina > GODZINA_ZACHOD ? 1f : 0f));

        RenderSettings.ambientSkyColor = Color.Lerp(skyDzien, skyNoc, nocTotal);
        RenderSettings.ambientEquatorColor = Color.Lerp(kolorDzien * 0.6f, kolorNoc * 0.25f, nocTotal);
        RenderSettings.ambientGroundColor = Color.Lerp(groundDzien, groundNoc, nocTotal);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 150f;
        RenderSettings.fogEndDistance = 900f;
        Color mglaDzien = new Color(0.72f, 0.68f, 0.6f);
        Color mglaNoc = new Color(0.08f, 0.09f, 0.13f);
        RenderSettings.fogColor = Color.Lerp(mglaDzien, mglaNoc, nocTotal);

        // Latarnie
        bool lampyWlaczone = godzina >= GODZINA_LAMPY;
#pragma warning disable CS0618 // FindObjectsOfType — kompatybilne ze starszym API edytora używanym w teście kompilacji
        var lampy = FindObjectsOfType<MapaLampa>();
#pragma warning restore CS0618
        foreach (var lampa in lampy)
        {
            if (!lampa.noc) continue;
            var l = lampa.GetComponent<Light>();
            if (l == null) continue;
            l.enabled = lampyWlaczone;
            if (lampyWlaczone) l.intensity = lampa.mocBazowa > 0f ? lampa.mocBazowa : l.intensity;
        }

        // Okna świetlące się emisją w nocy
        float emisja = lampyWlaczone ? 2f : 0f;
        foreach (var mat in materialyEmisjaNocna)
        {
            if (mat == null) continue;
            mat.SetColor("_EmissionColor", mat.GetColor("_EmissionColor").linear * 0f + GetBaseEmisja(mat) * emisja);
            if (emisja > 0f) mat.EnableKeyword("_EMISSION");
            else mat.DisableKeyword("_EMISSION");
        }

        UpewnijSiePostProcessing();
    }

    // Kolor emisji bazowy (bez intensywności) jest zapisany przy tworzeniu materiału
    // w kanale _EmissionColor znormalizowanym do 1 — tu tylko skalujemy intensywność.
    static Dictionary<Material, Color> _bazaEmisji = new Dictionary<Material, Color>();
    static Color GetBaseEmisja(Material m)
    {
        if (!_bazaEmisji.TryGetValue(m, out var c))
        {
            c = m.GetColor("_EmissionColor");
            if (c.maxColorComponent > 0f) c /= c.maxColorComponent;
            _bazaEmisji[m] = c;
        }
        return c;
    }

    void UpewnijSiePostProcessing()
    {
        var cam = Camera.main;
        if (cam == null) return;
        var dane = cam.GetComponent<UniversalAdditionalCameraData>();
        if (dane == null) dane = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        dane.renderPostProcessing = true;
    }
}
