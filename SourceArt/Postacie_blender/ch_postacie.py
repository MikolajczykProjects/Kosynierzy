# 21 postaci CH_* + CH_Animacje (wspolne klipy Humanoid). Wymaga ob_lib.py, ob_p2.py, ch_lib.py.
# Zadnych symboli: mundury SD szaro-zielone bez naszywek, czapki bez godel.
POSTACIE = {
    "CH_Kruk":        dict(wlosy="wlosy_ciemne", czapka="kapelusz", czapka_kol="plaszcz_brazowy", tulow="garnitur",
                           plaszcz="dlugi", plaszcz_kol="plaszcz_brazowy", spodnie="garnitur", krawat="granat", kolnierz="koszula"),
    "CH_Zofia":       dict(plec="K", wlosy="wlosy_siwe", fryzura="kok", tulow="granat", spodnica="sukienka_niebieska",
                           okulary=True, buty="skora", mankiet="granat", kolnierz="koszula"),
    "CH_Naujocks":    dict(wlosy="wlosy_jasne", tulow="spodnie_szare", spodnie="spodnie_szare", krawat="plaszcz_czarny", kolnierz="koszula"),
    "CH_Heinz":       dict(wlosy="wlosy_ciemne", czapka="kaszkiet", czapka_kol="garnitur", tulow="koszula", kamizelka="kamizelka",
                           spodnie="garnitur", rekawy="koszula"),
    "CH_SD":          dict(wlosy="wlosy_jasne", czapka="furazerka", czapka_kol="mundur_sd", tulow="mundur_sd", spodnie="mundur_sd",
                           buty_wysokie=True, pas="skora_czarna", mankiet="mundur_sd"),
    "CH_SD_Oficer":   dict(wlosy="wlosy_ciemne", czapka="oficerska", czapka_kol="mundur_sd", tulow="mundur_sd", spodnie="mundur_sd",
                           buty_wysokie=True, pas="skora_czarna", mankiet="mundur_sd", kolnierz="mundur_sd"),
    "CH_Schupo":      dict(wlosy="wlosy_ciemne", czapka="czako", czapka_kol="lakier_czarny", tulow="mundur_policja",
                           spodnie="mundur_policja", buty_wysokie=True, pas="skora_czarna", mankiet="mundur_policja", wasy="wlosy_ciemne"),
    "CH_Gestapo":     dict(wlosy="wlosy_ciemne", czapka="kapelusz", czapka_kol="plaszcz_czarny", tulow="garnitur",
                           plaszcz="dlugi", plaszcz_kol="skora_czarna", spodnie="plaszcz_czarny"),
    "CH_Gruber":      dict(wlosy="wlosy_siwe", tulow="koszula", plaszcz="dlugi", plaszcz_kol="kitel", spodnie="spodnie_szare",
                           okulary=True, wasy="wlosy_siwe"),
    "CH_Pracownik":   dict(wlosy="wlosy_jasne", tulow="koszula", kamizelka="garnitur", spodnie="spodnie_szare", rekawy="koszula"),
    "CH_Fotograf":    dict(wlosy="wlosy_siwe", fryzura="krotkie", tulow="koszula", kamizelka="kamizelka", spodnie="granat",
                           okulary=True, mucha="plaszcz_czarny", rekawy="koszula"),
    "CH_Kolejarz":    dict(wlosy="wlosy_ciemne", czapka="kolejarz", czapka_kol="granat", tulow="granat", spodnie="granat",
                           pas="skora_czarna", mankiet="granat", wasy="wlosy_ciemne"),
    "CH_Sowa":        dict(wlosy="wlosy_jasne", czapka="kaszkiet", czapka_kol="spodnie_szare", tulow="koszula",
                           plaszcz="krotki", plaszcz_kol="plaszcz_brazowy", spodnie="spodnie_szare", buty_wysokie=True, wasy="wlosy_jasne"),
    "CH_Recepcjonista": dict(wlosy="wlosy_ciemne", tulow="plaszcz_czarny", spodnie="plaszcz_czarny", mucha="plaszcz_czarny", kolnierz="koszula"),
    "CH_Cywil_M1":    dict(wlosy="wlosy_ciemne", czapka="kapelusz", czapka_kol="garnitur", tulow="garnitur", spodnie="garnitur",
                           krawat="sukienka_czerwona", kolnierz="koszula"),
    "CH_Cywil_M2":    dict(wlosy="wlosy_jasne", czapka="kaszkiet", czapka_kol="kamizelka", tulow="koszula", kamizelka="kamizelka",
                           spodnie="spodnie_szare", rekawy="koszula"),
    "CH_Cywil_M3":    dict(wlosy="wlosy_siwe", czapka="kapelusz", czapka_kol="plaszcz_czarny", tulow="garnitur", plaszcz="dlugi",
                           plaszcz_kol="spodnie_szare", spodnie="garnitur"),
    "CH_Cywil_K1":    dict(plec="K", wlosy="wlosy_ciemne", fryzura="dlugie", tulow="sukienka_czerwona", spodnica="sukienka_czerwona",
                           buty="skora", mankiet=None),
    "CH_Cywil_K2":    dict(plec="K", wlosy="wlosy_jasne", fryzura="kok", tulow="koszula", spodnica="sukienka_niebieska",
                           buty="skora_czarna", mankiet=None, rekawy="koszula"),
    "CH_Cywil_K3":    dict(plec="K", wlosy="wlosy_jasne", fryzura="krotkie", tulow="granat", plaszcz="dlugi",
                           plaszcz_kol="plaszcz_brazowy", spodnica="granat", buty="skora_czarna"),
    # v4: bez "okrycia" (plaska plyta przyklejona do tulowia psula kazda animacje).
    # Scena z okrytym Honiokiem = klip Lezy + osobny rekwizyt w scenie.
    "CH_Honiok":      dict(wlosy="wlosy_ciemne", czapka="kaszkiet", czapka_kol="plaszcz_brazowy", tulow="koszula",
                           kamizelka="plaszcz_brazowy", spodnie="spodnie_szare", rekawy="koszula"),
}


def zbuduj_postacie(tylko=None):
    wyniki = []
    for n, st in POSTACIE.items():
        if tylko and n not in tylko:
            continue
        C, arm, mesh = zloz_postac(n, st)
        tris = sum(len(p.vertices) - 2 for p in mesh.data.polygons)
        wyniki.append((n, tris))
    print("postacie:", wyniki)


def zbuduj_animacje():
    C, arm, mesh = zloz_postac("CH_Animacje", POSTACIE["CH_Kruk"])
    akcje = zrob_klipy_czlowieka(arm)
    print("animacje:", [(a.name, int(a.frame_range[1])) for a in akcje])
    return arm
