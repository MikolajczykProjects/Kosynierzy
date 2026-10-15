// Rola D — priorytet 2. Czesc klasy BudujObiekty (patrz BudujObiekty.cs).
// Osie: Blender (x, y, z) -> Unity (-x, z, -y). Przod obiektu = +Z.
#if UNITY_EDITOR
using UnityEditor.Animations;
using UnityEngine;

public static partial class BudujObiekty
{
    static void Priorytet2()
    {
        Amunicja();           // przed Skrytka (jest w srodku)
        Aparat();
        MapaStol();
        Kartkowe("OB_Lista");
        Kartkowe("OB_Kartka");
        Prosty("OB_Telefon");
        Skrytka();
        MikrofonBurzowy();
        Prosty("OB_Pulpit");
        Prosty("OB_Szafa_Aparatura");
        Latarnia();
        Pojazd("OB_Auto_SD", 0.36f, true);
        Prosty("OB_Beczka");
        Prosty("OB_Skrzynia");
        Prosty("OB_Worki");
    }

    static void Prosty(string nazwa)
    {
        var go = Model(nazwa); if (!go) return;
        Kolizja(go);
        Zapisz(go);
    }

    static void Amunicja() => Prosty("OB_Amunicja");

    static void Kartkowe(string nazwa)
    {
        var go = Model(nazwa); if (!go) return;
        var bc = Kolizja(go, null, true);
        bc.size = new Vector3(bc.size.x, 0.2f, bc.size.z); bc.center = new Vector3(bc.center.x, 0.1f, bc.center.z);
        Punkt(go, "CelZdjecia", new Vector3(0, 0.01f, 0));
        Zapisz(go);
    }

    static void Aparat()
    {
        var go = Model("OB_Aparat"); if (!go) return;
        Kolizja(go);
        var ob = Punkt(go, "Obiektyw", new Vector3(-0.005f, 0.035f, 0.06f));
        Wstaw("FX_FlashAparatu", ob);
        Zapisz(go);
    }

    static void MapaStol()
    {
        var go = Model("OB_Mapa_Stol"); if (!go) return;
        Kolizja(go, Znajdz(go.transform, "Stol"));
        Punkt(go, "CelZdjecia", new Vector3(0, 0.79f, 0));
        Zapisz(go);
    }

    static void Skrytka()
    {
        var go = Model("OB_Skrytka"); if (!go) return;
        Kolizja(go, Znajdz(go.transform, "Skrzynka"));
        var wn = Punkt(go, "Wnetrze", new Vector3(0, 0.03f, 0));
        var am = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{PRE}/OB_Amunicja.prefab");
        if (am)
        {
            var a = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(am);
            a.transform.SetParent(wn, false);
            var kol = a.GetComponent<Collider>();
            if (kol) kol.enabled = false;   // wieko ma byc klikalne, nie pudelko w srodku
        }
        const string W = "Wieko";
        var zam = Klip("Skrytka_Zamknieta", false, Obrot(W, Stala(0), Stala(0), Stala(0)));
        var otw = Klip("Skrytka_Otworz", false, Obrot(W, Kr(false, 0, 0, .45f, -112, .6f, -102, .7f, -105), Stala(0, .7f), Stala(0, .7f)));
        var ac = Kontroler("AC_Skrytka");
        ac.AddParameter("Otworz", AnimatorControllerParameterType.Trigger);
        var sm = ac.layers[0].stateMachine;
        var sZ = Stan(sm, "Zamknieta", zam, new Vector2(250, 0));
        var sO = Stan(sm, "Otwarta", otw, new Vector2(250, 120));
        sm.defaultState = sZ;
        Do(sZ, sO).AddCondition(AnimatorConditionMode.If, 0, "Otworz");
        DodajAnimator(go, ac);
        Zapisz(go);
    }

    static void MikrofonBurzowy()
    {
        var go = Model("OB_Mikrofon_Burzowy"); if (!go) return;
        Kolizja(go, Znajdz(go.transform, "Mikrofon"));
        Punkt(go, "CelZdjecia", new Vector3(0, 1.62f, 0));
        var e = Emisja["M_Lampka_Czerwona"];
        var nie = Klip("Mikrofon_Cisza", false, Emisja4("Lampka", Stala(0), Stala(0), Stala(0)));
        var tak = Klip("Mikrofon_Nadaje", true, Emisja4("Lampka",
            Schodki(0, e.r, .4f, 0, .8f, e.r), Schodki(0, e.g, .4f, 0, .8f, e.g), Schodki(0, e.b, .4f, 0, .8f, e.b)));
        var ac = Kontroler("AC_Mikrofon");
        ac.AddParameter("Nadaje", AnimatorControllerParameterType.Bool);
        BoolWlWyl(ac.layers[0].stateMachine, "Nadaje", nie, tak, false);
        DodajAnimator(go, ac);
        Zapisz(go);
    }

    static void Latarnia()
    {
        var go = Model("OB_Latarnia"); if (!go) return;
        var bc = go.AddComponent<BoxCollider>();
        bc.center = new Vector3(0, 2.1f, 0); bc.size = new Vector3(0.3f, 4.2f, 0.3f);
        var l = Swiatlo(go.transform, "Swiatlo", LightType.Spot, new Color(1f, 0.78f, 0.45f), 8f, 3f,
                        new Vector3(0, 3.6f, 0), new Vector3(90, 0, 0), 110f);
        _ = l;
        var e = Emisja["M_Klosz"];
        var wl = Klip("Latarnia_Wlaczona", true,
            Emisja4("Klosz", Stala(e.r), Stala(e.g), Stala(e.b)),
            J(Wlacz("Swiatlo", typeof(Light), Stala(1)),
              new K("Swiatlo", typeof(Light), "m_Intensity", Kr(false, 0, 3f, .7f, 2.8f, 1.1f, 3.1f, 1.6f, 2.6f, 1.7f, 3f, 2.6f, 2.9f, 3.2f, 3f))));
        var wy = Klip("Latarnia_Wylaczona", false,
            Emisja4("Klosz", Stala(0), Stala(0), Stala(0)),
            J(Wlacz("Swiatlo", typeof(Light), Stala(0))));
        var ac = Kontroler("AC_Latarnia");
        ac.AddParameter(new AnimatorControllerParameter { name = "Wlaczona", type = AnimatorControllerParameterType.Bool, defaultBool = true });
        BoolWlWyl(ac.layers[0].stateMachine, "Wlaczona", wy, wl, true);
        DodajAnimator(go, ac);
        Zapisz(go);
    }

    // Auto_SD i Ciezarowka: Speed (kola), Swiatla (bool), opcjonalnie OtworzDrzwi (trigger)
    static void Pojazd(string nazwa, float promienKola, bool drzwi)
    {
        var go = Model(nazwa); if (!go) return;
        string pre = nazwa.Substring(3);
        Kolizja(go, Znajdz(go.transform, "Karoseria"));
        var sl = Znajdz(go.transform, "Swiatla_L");
        var sp = Znajdz(go.transform, "Swiatla_P");
        Swiatlo(sl, "Reflektor", LightType.Spot, new Color(1f, 0.92f, 0.7f), 20f, 3f, Vector3.zero, new Vector3(8, 0, 0), 50f);
        Swiatlo(sp, "Reflektor", LightType.Spot, new Color(1f, 0.92f, 0.7f), 20f, 3f, Vector3.zero, new Vector3(8, 0, 0), 50f);

        var ac = Kontroler("AC_" + pre);
        ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
        if (drzwi) ac.AddParameter("OtworzDrzwi", AnimatorControllerParameterType.Trigger);
        ac.AddParameter("Swiatla", AnimatorControllerParameterType.Bool);

        // warstwa 0: kola. Klip = 1 obrot w czasie = obwod kola [m], mnoznik = Speed [m/s]
        float obw = 2f * Mathf.PI * promienKola;
        var kola = new System.Collections.Generic.List<K[]>();
        foreach (var k in new[] { "Kolo_PP", "Kolo_LP", "Kolo_PT", "Kolo_LT" })
            kola.Add(Obrot(k, Kr(true, 0, 0, obw, 360), Stala(0, obw), Stala(0, obw)));
        var kKola = Klip(pre + "_Kola", true, kola.ToArray());
        var sKola = Stan(ac.layers[0].stateMachine, "Jazda", kKola, new Vector2(250, 0));
        sKola.speedParameterActive = true; sKola.speedParameter = "Speed";

        // warstwa: drzwi kierowcy (otwiera, czeka 1 s, zamyka)
        if (drzwi)
        {
            var wD = NowaWarstwa(ac, "Drzwi");
            const string D = "Drzwi_Kierowcy";
            var dZ = Klip(pre + "_Drzwi_Zamkniete", false, Obrot(D, Stala(0), Stala(0), Stala(0)));
            var dO = Klip(pre + "_Drzwi_Otworz", false, Obrot(D, Stala(0, 1.8f), Kr(false, 0, 0, .4f, 70, 1.4f, 70, 1.8f, 0), Stala(0, 1.8f)));
            var smD = wD.stateMachine;
            var sDZ = Stan(smD, "Zamkniete", dZ, new Vector2(250, 0));
            var sDO = Stan(smD, "Otwieranie", dO, new Vector2(250, 120));
            smD.defaultState = sDZ;
            Do(sDZ, sDO).AddCondition(AnimatorConditionMode.If, 0, "OtworzDrzwi");
            Do(sDO, sDZ, 0f, true);

        }

        // warstwa: swiatla
        var wS = NowaWarstwa(ac, "Swiatla");
        var e = Emisja["M_Reflektor"];
        var sWl = Klip(pre + "_Swiatla_Wl", false, Emisja4("Reflektory", Stala(e.r), Stala(e.g), Stala(e.b)),
            J(Wlacz("Swiatla_L/Reflektor", typeof(Light), Stala(1)), Wlacz("Swiatla_P/Reflektor", typeof(Light), Stala(1))));
        var sWy = Klip(pre + "_Swiatla_Wyl", false, Emisja4("Reflektory", Stala(0), Stala(0), Stala(0)),
            J(Wlacz("Swiatla_L/Reflektor", typeof(Light), Stala(0)), Wlacz("Swiatla_P/Reflektor", typeof(Light), Stala(0))));
        BoolWlWyl(wS.stateMachine, "Swiatla", sWy, sWl, false);

        DodajAnimator(go, ac);
        Zapisz(go);
    }
}
#endif
