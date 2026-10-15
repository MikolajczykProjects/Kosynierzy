using UnityEngine;

/// <summary>
/// Oznacza światło latarni/lampy na mapie. "noc" = światło ma się zapalać dopiero wieczorem
/// (ustawiane przez MapaSwiatlo.Ustaw). Jeśli false, światło świeci zawsze (np. wnętrza).
/// </summary>
[RequireComponent(typeof(Light))]
public class MapaLampa : MonoBehaviour
{
    public bool noc = true;

    [HideInInspector] public float mocBazowa = -1f;

    void Awake()
    {
        var l = GetComponent<Light>();
        if (l != null && mocBazowa < 0f) mocBazowa = l.intensity;
    }
}
