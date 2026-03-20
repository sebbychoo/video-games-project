using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Adds a colored outline to a 3D mesh OR a 2D sprite.
    /// - Mesh: spawns a scaled child with front-face culling
    /// - Sprite: spawns 4 offset copies of the sprite behind the original
    /// Attach to the same GameObject as your Renderer.
    /// </summary>
    public class OutlineEffect : MonoBehaviour
    {
        [SerializeField] Color outlineColor     = Color.red;
        [SerializeField] float outlineThickness = 0.05f;

        private GameObject _outlineObj;
        private Material   _outlineMat;

        // Sprite outline
        private GameObject[] _spriteOutlines;
        private bool _isSprite;

        private void Awake()
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                BuildSpriteOutline(sr);
                _isSprite = true;
            }
            else
            {
                BuildMeshOutline();
                _isSprite = false;
            }
        }

        private void BuildMeshOutline()
        {
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
            _outlineMat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Front);

            MeshRenderer mr = _outlineObj.AddComponent<MeshRenderer>();
            mr.material = _outlineMat;

            _outlineObj.SetActive(false);
        }

        private void BuildSpriteOutline(SpriteRenderer source)
        {
            // 4 offset copies: up, down, left, right
            Vector3[] offsets = {
                Vector3.up * outlineThickness,
                Vector3.down * outlineThickness,
                Vector3.left * outlineThickness,
                Vector3.right * outlineThickness
            };

            // Create a material that renders the sprite as a solid color
            _outlineMat = new Material(Shader.Find("GUI/Text Shader"));

            _spriteOutlines = new GameObject[4];
            for (int i = 0; i < 4; i++)
            {
                GameObject go = new GameObject($"_SpriteOutline_{i}");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = offsets[i];
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;

                SpriteRenderer outlineSR = go.AddComponent<SpriteRenderer>();
                outlineSR.sprite = source.sprite;
                outlineSR.sortingLayerID = source.sortingLayerID;
                outlineSR.sortingOrder = source.sortingOrder - 1;
                outlineSR.material = _outlineMat;

                go.SetActive(false);
                _spriteOutlines[i] = go;
            }
        }

        private void OnDestroy()
        {
            if (_outlineMat != null)
                Destroy(_outlineMat);
        }

        public void ShowOutline(Color color)
        {
            if (_isSprite)
            {
                if (_spriteOutlines == null) return;
                if (_outlineMat != null)
                    _outlineMat.color = color;
                SpriteRenderer source = GetComponent<SpriteRenderer>();
                foreach (var go in _spriteOutlines)
                {
                    if (go == null) continue;
                    SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                    if (source != null) sr.sprite = source.sprite;
                    go.SetActive(true);
                }
            }
            else
            {
                if (_outlineObj == null) return;
                _outlineMat.color = color;
                _outlineObj.SetActive(true);
            }
        }

        public void HideOutline()
        {
            if (_isSprite)
            {
                if (_spriteOutlines == null) return;
                foreach (var go in _spriteOutlines)
                    if (go != null) go.SetActive(false);
            }
            else
            {
                if (_outlineObj == null) return;
                _outlineObj.SetActive(false);
            }
        }
    }
}
