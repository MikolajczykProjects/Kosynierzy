using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Dodawany na obiekt "Punkty". Rysuje w edytorze kulkę + strzałkę yaw dla każdego dziecka
/// (spawn/drzwi/sd/karta po prefiksie nazwy). W Play tryb, klawisz F2 chowa/pokazuje gizma.
/// </summary>
public class PunktyGizmos : MonoBehaviour
{
    public bool widoczne = true;

    static Color KolorDlaNazwy(string nazwa)
    {
        string n = nazwa.ToLowerInvariant();
        if (n.StartsWith("spawn")) return Color.green;
        if (n.StartsWith("drzwi") || n.Contains("_drzwi")) return Color.yellow;
        if (n.StartsWith("sd")) return Color.red;
        if (n.StartsWith("karta")) return Color.blue;
        return Color.white;
    }

    void Update()
    {
        bool nacisnieto = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame) nacisnieto = true;
#else
        if (Input.GetKeyDown(KeyCode.F2)) nacisnieto = true;
#endif
        if (nacisnieto) widoczne = !widoczne;
    }

    void OnDrawGizmos()
    {
        if (!widoczne) return;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform t = transform.GetChild(i);
            Color kolor = KolorDlaNazwy(t.name);
            Gizmos.color = kolor;
            Gizmos.DrawSphere(t.position, 0.4f);

            Vector3 strzalka = t.position + t.forward * 1.6f;
            Gizmos.DrawLine(t.position, strzalka);
            Vector3 lewo = Quaternion.LookRotation(t.position - strzalka) * Quaternion.Euler(0, 25, 0) * Vector3.forward * 0.4f;
            Vector3 prawo = Quaternion.LookRotation(t.position - strzalka) * Quaternion.Euler(0, -25, 0) * Vector3.forward * 0.4f;
            Gizmos.DrawLine(strzalka, strzalka + lewo);
            Gizmos.DrawLine(strzalka, strzalka + prawo);

#if UNITY_EDITOR
            UnityEditor.Handles.color = kolor;
            UnityEditor.Handles.Label(t.position + Vector3.up * 0.6f, t.name);
#endif
        }
    }
}
