using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Colours a placeholder part through a MaterialPropertyBlock (shared default material, no material assets needed).
    /// The colour is serialized and re-applied on enable, also in edit mode, so placed placeholders keep their look.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class PlaceholderTint : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");

        [SerializeField] Color color = Color.gray;

        public Color Color
        {
            get => color;
            set
            {
                color = value;
                Apply();
            }
        }

        void OnEnable() => Apply();
        void OnValidate() => Apply();

        public void Apply()
        {
            var r = GetComponent<Renderer>();
            if (r == null) return;
            var block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            block.SetFloat(SmoothnessId, 0.12f);
            r.SetPropertyBlock(block);
        }
    }
}
