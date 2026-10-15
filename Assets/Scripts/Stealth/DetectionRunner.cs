using UnityEngine;

namespace Kosynierzy
{
    /// <summary>Created automatically by Detection: refreshes it every frame so StatusChanged fires even when nobody reads it.</summary>
    [DefaultExecutionOrder(100)]
    public class DetectionRunner : MonoBehaviour
    {
        void LateUpdate() => Detection.Refresh();
    }
}
