using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Moves the EnemyHUDPanel each frame so it tracks the live enemy in world space.
    /// Queries EnemyHPBar for the currently tracked EnemyCombatant, converts that
    /// transform's world position to canvas local space, and repositions this panel.
    /// Panel pivot must be (0.5, 1) — top-center — so the panel hangs below the target point.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class EnemyHUDFollow : MonoBehaviour
    {
        [SerializeField] private EnemyHPBar hpBar;

        [Tooltip("World-space offset applied to the enemy position before projecting to screen. " +
                 "Negative Y moves the panel below the enemy's transform origin.")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, -1.2f, 0f);

        private RectTransform _rt;
        private RectTransform _canvasRT;
        private Canvas        _canvas;
        private Camera        _cam;

        private void Awake()
        {
            _rt     = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null)
                _canvasRT = _canvas.GetComponent<RectTransform>();
        }

        /// <summary>Wire this at runtime when the enemy is spawned if hpBar is already known.</summary>
        public void SetHPBar(EnemyHPBar bar) => hpBar = bar;

        private void Update()
        {
            if (hpBar == null || hpBar.TrackedEnemy == null) return;
            if (_canvasRT == null) return;

            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            Vector3 worldPos   = hpBar.TrackedEnemy.transform.position + worldOffset;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(_cam, worldPos);

            Camera eventCam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _cam;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRT, screenPoint, eventCam, out Vector2 localPos))
            {
                _rt.anchoredPosition = localPos;
            }
        }
    }
}
