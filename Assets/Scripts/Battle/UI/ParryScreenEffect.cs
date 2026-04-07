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
        [SerializeField] Color parrySuccessColor = new Color(0, 1, 0, 0.4f);
        [SerializeField] float flashDuration = 0.15f;
        [SerializeField] float invertFlashDuration = 0.2f;

        private GameObject _grayscaleVolObj;
        private GameObject _invertVolObj;
        private Coroutine _flashRoutine;

        private void Awake()
        {
            if (flashOverlay != null)
                flashOverlay.gameObject.SetActive(false);

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

            Debug.Log($"[ParryEffect] Grayscale: {(_grayscaleVolObj != null ? _grayscaleVolObj.name : "NOT FOUND")}, Invert: {(_invertVolObj != null ? _invertVolObj.name : "NOT FOUND")}");
        }

        [Header("Warning (during fast dash, before parry window)")]
        [Tooltip("Warning icon sprite (like a ⚠ symbol). Tinted to attack color.")]
        [SerializeField] Image warningIcon;
        [Tooltip("Full-screen overlay tinted to attack color during warning.")]
        [SerializeField] Image warningOverlay;
        [SerializeField] float warningOverlayAlpha = 0.2f;

        [Header("Intent Color Mapping")]
        [SerializeField] Color whiteIntentColor = Color.white;
        [SerializeField] Color yellowIntentColor = new Color(1f, 0.85f, 0f);
        [SerializeField] Color redIntentColor = new Color(1f, 0.2f, 0.2f);

        public void ShowWarning(IntentColor intentColor = IntentColor.White)
        {
            Color tint = GetIntentColor(intentColor);

            if (warningIcon != null)
            {
                warningIcon.color = tint;
                warningIcon.gameObject.SetActive(true);
            }
            if (warningOverlay != null)
            {
                Color overlayTint = tint;
                overlayTint.a = warningOverlayAlpha;
                warningOverlay.color = overlayTint;
                warningOverlay.gameObject.SetActive(true);
            }
        }

        public void HideWarning()
        {
            if (warningIcon != null)
                warningIcon.gameObject.SetActive(false);
            if (warningOverlay != null)
                warningOverlay.gameObject.SetActive(false);
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
