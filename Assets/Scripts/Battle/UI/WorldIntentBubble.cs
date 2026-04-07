using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Attaches to a character and shows their intent as a world-space sprite
    /// that always faces the camera. Like a thought bubble floating near their head.
    /// 
    /// For enemies: top-left of head.
    /// For player: to the right of head.
    /// </summary>
    public class WorldIntentBubble : MonoBehaviour
    {
        [Header("Positioning")]
        [Tooltip("Offset from the character's pivot. For enemies use (-0.3, 1.2, 0), for player use (0.5, 1.0, 0).")]
        [SerializeField] Vector3 offset = new Vector3(-0.3f, 1.2f, 0f);

        [Tooltip("Scale of the intent sprite in world units.")]
        [SerializeField] float spriteScale = 0.4f;

        [Header("References")]
        [Tooltip("Leave null — auto-created at runtime.")]
        [SerializeField] SpriteRenderer intentRenderer;

        private Transform _camera;
        private GameObject _bubbleObj;

        private void Start()
        {
            _camera = Camera.main != null ? Camera.main.transform : null;

            if (intentRenderer == null)
            {
                _bubbleObj = new GameObject("IntentBubble");
                _bubbleObj.transform.SetParent(transform, false);
                _bubbleObj.transform.localPosition = offset;
                _bubbleObj.transform.localScale = Vector3.one * spriteScale;

                intentRenderer = _bubbleObj.AddComponent<SpriteRenderer>();
                intentRenderer.sortingOrder = 100; // render on top
            }
        }

        private void LateUpdate()
        {
            if (_bubbleObj == null || _camera == null) return;

            // Keep position relative to parent
            _bubbleObj.transform.position = transform.position + offset;

            // Billboard — always face the camera
            _bubbleObj.transform.rotation = _camera.rotation;
        }

        /// <summary>
        /// Set the intent icon. Pass null to hide.
        /// </summary>
        public void SetIntent(Sprite intentSprite)
        {
            if (intentRenderer == null) return;

            if (intentSprite != null)
            {
                intentRenderer.sprite = intentSprite;
                intentRenderer.enabled = true;
            }
            else
            {
                intentRenderer.enabled = false;
            }
        }

        /// <summary>
        /// Hide the intent bubble.
        /// </summary>
        public void Hide()
        {
            if (intentRenderer != null)
                intentRenderer.enabled = false;
        }
    }
}
