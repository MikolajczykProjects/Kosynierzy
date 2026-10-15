// Rola D — priorytet 3. Czesc klasy BudujObiekty (patrz BudujObiekty.cs).
// Osie: Blender (x, y, z) -> Unity (-x, z, -y). Przod obiektu = +Z.
#if UNITY_EDITOR
using UnityEditor.Animations;
using UnityEngine;

public static partial class BudujObiekty
{
    static void Priorytet3()
    {
        Pojazd("OB_Ciezarowka", 0.46f, true);
        Rower();
        foreach (var n in new[] {
            "OB_Film", "OB_Radio_Odbiornik",
            "OB_Stol", "OB_Krzeslo", "OB_Lozko", "OB_Szafa", "OB_Biurko", "OB_Lada_Recepcja",
            "OB_Regal_Ksiazki", "OB_Kanapa", "OB_Lustro",
            "OB_Lawka", "OB_Kiosk", "OB_Slup_Ogloszeniowy", "OB_Hydrant", "OB_Kosz", "OB_Wozek_Reczny",
            "OB_Plakat", "OB_Wagon" })
            Prosty(n);
        LampaBiurkowa();
        LampaCiemnia();
        AparatStudyjny();
        Lokomotywa();
    }

    static void Rower()
    {
        var go = Model("OB_Rower"); if (!go) return;
        Kolizja(go);
        Punkt(go, "Siodelko", new Vector3(0, 0.97f, -0.24f));
        // 1 obrot kola = obwod (2*pi*0,34 m) sekund przy Speed = 1 m/s; pedaly pol obrotu (dwie korby -> bez skoku)
        float obw = 2f * Mathf.PI * 0.34f;
        var klip = Klip("Rower_Jazda", true,
            Obrot("Kolo_P", Kr(true, 0, 0, obw, 360), Stala(0, obw), Stala(0, obw)),
            Obrot("Kolo_T", Kr(true, 0, 0, obw, 360), Stala(0, obw), Stala(0, obw)),
            Obrot("Pedaly", Kr(true, 0, 0, obw, 180), Stala(0, obw), Stala(0, obw)));
        var ac = Kontroler("AC_Rower");
        ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
        var s = Stan(ac.layers[0].stateMachine, "Jazda", klip, new Vector2(250, 0));
        s.speedParameterActive = true; s.speedParameter = "Speed";
        DodajAnimator(go, ac);
        Zapisz(go);
    }

    static void LampaBiurkowa()
    {
        var go = Model("OB_Lampa_Biurkowa"); if (!go) return;
        Kolizja(go);
        Swiatlo(go.transform, "Swiatlo", LightType.Point, new Color(1f, 0.8f, 0.5f), 3f, 1f, new Vector3(0, 0.3f, 0.15f), Vector3.zero);
        Zapisz(go);
    }

    static void LampaCiemnia()
    {
        var go = Model("OB_Lampa_Ciemnia"); if (!go) return;
        Kolizja(go);
        Swiatlo(go.transform, "Swiatlo", LightType.Point, new Color(1f, 0.08f, 0.04f), 4f, 1.5f, new Vector3(0, 0.15f, 0), Vector3.zero);
        Zapisz(go);
    }

    static void AparatStudyjny()
    {
        var go = Model("OB_Aparat_Studyjny"); if (!go) return;
        Kolizja(go);
        var ob = Punkt(go, "Obiektyw", new Vector3(0, 1.42f, 0.41f));
        Wstaw("FX_FlashAparatu", ob);
        Zapisz(go);
    }

    static void Lokomotywa()
    {
        var go = Model("OB_Lokomotywa"); if (!go) return;
        Kolizja(go);
        var komin = Punkt(go, "Komin", new Vector3(0, 3.95f, 4.9f));
        Wstaw("FX_Dym", komin);   // petla, Play On Awake ON
        Zapisz(go);
    }
}
#endif
