using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace CardBattle
{
    /// <summary>
    /// Parry visual effects.
    /// For B&W: uses a Volume you set up manually in the scene with saturation -100.
    /// This script finds it by tag "ParryVolume" and toggles its weight.
    /// For flashes: uses a full-screen UI overlay.
    /// </summary>
    public class ParryScreenEffect : MonoBehaviour
    {
        [Header("Volume Tags")]
        [Tooltip("Tag of the B&W Volume (saturation -100, weight 1). Starts inactive.")]
        [SerializeField] string grayscaleVolumeTag = "ParryVolume";

        [Tooltip("Tag of the Invert Volume (Color Filter inverted + high contrast). Starts inactive.")]
        [SerializeField] string invertVolumeTag = "InvertVolume";

        [Header("Flash Overlay")]
        [SerializeField] Image flashOverlay;

        [Header("Flash Settings")]
        [SerializeField] Color parrySuccessColor = new Color(0, 1, 0, 0.55f);
        [SerializeField] float flashDuration = 0.15f;
        [SerializeField] float invertFlashDuration = 0.2f;

        private GameObject _grayscaleVolObj;
        private GameObject _invertVolObj;
        private Coroutine _flashRoutine;

        private Canvas _flashOverlayCanvas;

        private void Awake()
        {
            if (flashOverlay != null)
            {
                flashOverlay.gameObject.SetActive(false);

                // If the flash overlay's parent Canvas is Screen Space - Camera,
                // the 3D scene lighting bleeds through even at alpha 1.0.
                // Fix: give the flash overlay its own Canvas in Overlay mode.
                Canvas parentCanvas = flashOverlay.GetComponentInParent<Canvas>();
                if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    GameObject overlayGO = new GameObject("ParryFlashCanvas");
                    overlayGO.transform.SetParent(parentCanvas.transform.parent, false);
                    _flashOverlayCanvas = overlayGO.AddComponent<Canvas>();
                    _flashOverlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    _flashOverlayCanvas.sortingOrder = 998;
                    overlayGO.AddComponent<CanvasScaler>();

                    // Reparent the flash overlay Image to the new overlay canvas
                    flashOverlay.transform.SetParent(_flashOverlayCanvas.transform, false);

                    // Stretch to fill
                    RectTransform rt = flashOverlay.rectTransform;
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
            }

            // Find volumes while they might be active, then deactivate
            _grayscaleVolObj = GameObject.FindWithTag(grayscaleVolumeTag);
            _invertVolObj = GameObject.FindWithTag(invertVolumeTag);

            // If not found (they started inactive), search all objects manually
            if (_grayscaleVolObj == null || _invertVolObj == null)
            {
                foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
                {
                    if (go.scene.name == null) continue; // skip assets
                    if (_grayscaleVolObj == null && go.CompareTag(grayscaleVolumeTag))
                        _grayscaleVolObj = go;
                    if (_invertVolObj == null && go.CompareTag(invertVolumeTag))
                        _invertVolObj = go;
                }
            }

            if (_grayscaleVolObj != null) _grayscaleVolObj.SetActive(false);
            if (_invertVolObj != null) _invertVolObj.SetActive(false);

            // Also reparent the warning overlay to the overlay canvas if it's separate
            if (warningOverlay != null && warningOverlay != flashOverlay)
                ReparentToOverlayCanvas(warningOverlay);

            // Reparent warning icon to the overlay canvas so it renders on top of the flash
            if (warningIcon != null && _flashOverlayCanvas != null)
            {
                warningIcon.transform.SetParent(_flashOverlayCanvas.transform, false);
                // Keep the icon's anchoring as-is (centered or wherever it was designed)
                warningIcon.transform.SetAsLastSibling(); // render on top of overlay
            }

            Debug.Log($"[ParryEffect] Grayscale: {(_grayscaleVolObj != null ? _grayscaleVolObj.name : "NOT FOUND")}, Invert: {(_invertVolObj != null ? _invertVolObj.name : "NOT FOUND")}");
        }

        [Header("Warning (during fast dash, before parry window)")]
        [Tooltip("Warning icon sprite (like a ⚠ symbol). Tinted to attack color.")]
        [SerializeField] Image warningIcon;
        [Tooltip("Full-screen overlay tinted to attack color during warning. Falls back to flashOverlay if not assigned.")]
        [SerializeField] Image warningOverlay;
        [SerializeField] float warningOverlayAlpha = 0.35f;

        /// <summary>Returns the best available full-screen overlay for the warning tint.</summary>
        private Image WarningOverlayImage
        {
            get
            {
                if (warningOverlay != null) return warningOverlay;
                return flashOverlay;
            }
        }

        private void ReparentToOverlayCanvas(Image img)
        {
            if (img == null || _flashOverlayCanvas == null) return;
            // Only reparent if not already under the overlay canvas
            if (img.transform.IsChildOf(_flashOverlayCanvas.transform)) return;

            img.transform.SetParent(_flashOverlayCanvas.transform, false);
            RectTransform rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        [Header("Intent Color Mapping")]
        [SerializeField] Color whiteIntentColor = Color.white;
        [SerializeField] Color yellowIntentColor = new Color(1f, 0.85f, 0f);
        [SerializeField] Color redIntentColor = new Color(1f, 0.2f, 0.2f);

        public void ShowWarning(IntentColor intentColor = IntentColor.White)
        {
            Color tint = GetIntentColor(intentColor);

            // Overlay is on a Screen Space - Overlay canvas, so semi-transparency
            // looks uniform (no 3D lighting bleed).
            Image overlay = WarningOverlayImage;
            if (overlay != null)
            {
                Color overlayTint = tint;
                overlayTint.a = warningOverlayAlpha;
                overlay.color = overlayTint;
                overlay.gameObject.SetActive(true);
            }

            // Show warning icon on top of the overlay — both appear and disappear together
            if (warningIcon != null)
            {
                warningIcon.color = tint;
                warningIcon.gameObject.SetActive(true);
                warningIcon.transform.SetAsLastSibling();
            }
        }

        public void HideWarning()
        {
            Image overlay = WarningOverlayImage;
            if (overlay != null)
                overlay.gameObject.SetActive(false);

            if (warningIcon != null)
                warningIcon.gameObject.SetActive(false);
        }

        private Color GetIntentColor(IntentColor intent)
        {
            switch (intent)
            {
                case IntentColor.Yellow: return yellowIntentColor;
                case IntentColor.Red: return redIntentColor;
                default: return whiteIntentColor;
            }
        }

        [Header("Transition")]
        [Tooltip("Brief white flash before B&W kicks in, like a camera flash.")]
        [SerializeField] float transitionFlashDuration = 0.08f;
        [SerializeField] Color transitionFlashColor = new Color(1, 1, 1, 0.6f);

        public void StartParryWindow()
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(TransitionToGrayscale());
        }

        public void EndParryWindow()
        {
            if (_grayscaleVolObj != null) _grayscaleVolObj.SetActive(false);
        }

        private IEnumerator TransitionToGrayscale()
        {
            // Brief white flash
            if (flashOverlay != null)
            {
                flashOverlay.gameObject.SetActive(true);
                flashOverlay.color = transitionFlashColor;

                float elapsed = 0f;
                while (elapsed < transitionFlashDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / transitionFlashDuration;
                    Color c = transitionFlashColor;
                    c.a = Mathf.Lerp(transitionFlashColor.a, 0f, t);
                    flashOverlay.color = c;
                    yield return null;
                }
                flashOverlay.gameObject.SetActive(false);
            }

            // Now snap to B&W
            if (_grayscaleVolObj != null) _grayscaleVolObj.SetActive(true);
            _flashRoutine = null;
        }

        public void FlashParrySuccess()
        {
            EndParryWindow();
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine(parrySuccessColor, flashDuration));
        }

        public void FlashParryFail()
        {
            EndParryWindow();
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(InvertFlashRoutine());
        }

        private IEnumerator FlashRoutine(Color flashColor, float duration)
        {
            if (flashOverlay == null) yield break;

            // The overlay is on a Screen Space - Overlay canvas so semi-transparency
            // looks uniform (no 3D lighting bleed). Use the color's original alpha.
            flashOverlay.gameObject.SetActive(true);
            flashOverlay.color = flashColor;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                Color c = flashColor;
                c.a = Mathf.Lerp(flashColor.a, 0f, t);
                flashOverlay.color = c;
                yield return null;
            }

            flashOverlay.gameObject.SetActive(false);
            _flashRoutine = null;
        }

        private IEnumerator InvertFlashRoutine()
        {
            if (_invertVolObj != null) _invertVolObj.SetActive(true);
            else Debug.LogWarning("[ParryEffect] Invert volume not found!");

            yield return new WaitForSeconds(invertFlashDuration);

            if (_invertVolObj != null) _invertVolObj.SetActive(false);
            _flashRoutine = null;
        }
    }
}
