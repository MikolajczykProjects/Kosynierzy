using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Znacznik przypisujący obiekt/punkt do jednego lub więcej aktów fabuły.
/// Wyłącznie informacyjny — nie wpływa na grę w runtime.
/// </summary>
public class AktZnacznik : MonoBehaviour
{
    public int[] akty;
    public string rola;
    public string opisDlaAi;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (akty == null || akty.Length == 0) return;
        string etykieta = "AKT " + string.Join("-", akty);
        if (!string.IsNullOrEmpty(rola)) etykieta += " – " + rola;
        Handles.Label(transform.position + Vector3.up * 0.3f, etykieta);
    }
#endif
}
