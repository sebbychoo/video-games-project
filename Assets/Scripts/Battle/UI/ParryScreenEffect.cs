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
        }

        private GameObject FindVolume(string tag, ref GameObject cached)
        {
            if (cached != null) return cached;
            cached = GameObject.FindWithTag(tag);
            return cached;
        }

        public void StartParryWindow()
        {
            GameObject vol = FindVolume(grayscaleVolumeTag, ref _grayscaleVolObj);
            if (vol != null) vol.SetActive(true);
        }

        public void EndParryWindow()
        {
            GameObject vol = FindVolume(grayscaleVolumeTag, ref _grayscaleVolObj);
            if (vol != null) vol.SetActive(false);
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
            // Turn on the invert volume for a brief flash
            GameObject vol = FindVolume(invertVolumeTag, ref _invertVolObj);
            if (vol != null) vol.SetActive(true);

            yield return new WaitForSeconds(invertFlashDuration);

            if (vol != null) vol.SetActive(false);
            _flashRoutine = null;
        }
    }
}
