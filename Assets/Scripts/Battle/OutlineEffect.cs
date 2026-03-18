using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Adds a colored outline to a 3D object by spawning a slightly scaled
    /// child mesh with inverted normals. Works in URP without any custom shader.
    /// Attach to the same GameObject as your MeshFilter/Renderer.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class OutlineEffect : MonoBehaviour
    {
        [SerializeField] Color  outlineColor     = Color.red;
        [SerializeField] float  outlineThickness = 0.05f;

        private GameObject _outlineObj;
        private Material   _outlineMat;

        private void Awake()
        {
            // Build the outline child once
            MeshFilter sourceMesh = GetComponent<MeshFilter>();
            if (sourceMesh == null) return;

            _outlineObj = new GameObject("_Outline");
            _outlineObj.transform.SetParent(transform, false);
            _outlineObj.transform.localPosition = Vector3.zero;
            _outlineObj.transform.localRotation = Quaternion.identity;
            _outlineObj.transform.localScale    = Vector3.one * (1f + outlineThickness);

            MeshFilter mf = _outlineObj.AddComponent<MeshFilter>();
            mf.sharedMesh = sourceMesh.sharedMesh;

            _outlineMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _outlineMat.color = outlineColor;
            // Render back faces only so the outline shows around the edges
            _outlineMat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Front);

            MeshRenderer mr = _outlineObj.AddComponent<MeshRenderer>();
            mr.material = _outlineMat;

            _outlineObj.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_outlineMat != null)
                Destroy(_outlineMat);
        }

        public void ShowOutline(Color color)
        {
            if (_outlineObj == null) return;
            _outlineMat.color = color;
            _outlineObj.SetActive(true);
        }

        public void HideOutline()
        {
            if (_outlineObj == null) return;
            _outlineObj.SetActive(false);
        }
    }
}
