using UnityEngine;

namespace Procedural
{
    /// <summary>
    /// Randomly picks a material from a list and applies it to this object's Renderer.
    /// Use for computers, posters, desk items — anything that should have random art variants.
    /// </summary>
    public class RandomTexturePicker : MonoBehaviour
    {
        [Tooltip("List of materials to randomly choose from. Each should have your art texture assigned.")]
        [SerializeField] Material[] variants;

        private void Start()
        {
            if (variants == null || variants.Length == 0) return;

            Renderer rend = GetComponent<Renderer>();
            if (rend == null) return;

            rend.material = variants[Random.Range(0, variants.Length)];
        }
    }
}
