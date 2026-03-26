using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    public class CardAnimator : MonoBehaviour
    {
        [SerializeField] float entranceDuration  = 0.18f;
        [SerializeField] float staggerDelay      = 0.08f;
        [SerializeField] float startHeightOffset = 900f;
        [SerializeField] AnimationCurve entranceCurve;

        // Built-in slam curve: fast drop, tiny bounce at end
        private AnimationCurve SlamCurve => new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 8f),
            new Keyframe(0.75f, 1.05f, 2f, 0f),
            new Keyframe(1f, 1f, 0f, 0f)
        );

        [SerializeField] float hoverDuration   = 0.15f;
        [SerializeField] float hoverLiftHeight = 80f;
        [SerializeField] float hoverScale      = 1.25f;
        [SerializeField] float defaultXTilt    = 15f;

        [SerializeField] float   exitDuration      = 0.25f;
        [SerializeField] Vector2 battlefieldTarget;

        [SerializeField] float shakeDuration  = 0.3f;
        [SerializeField] float shakeMagnitude = 8f;

        [Header("Select Pop")]
        [SerializeField] float selectScale     = 1.4f;
        [SerializeField] float selectShakeDur  = 0.25f;
        [SerializeField] float selectShakeMag  = 6f;

        [Header("Selected Idle")]
        [SerializeField] float selectedLiftExtra   = 50f;
        [SerializeField] float selectedIdleShakeMag = 6f;
        [SerializeField] float selectedIdleSpeed    = 8f;

        private readonly Dictionary<CardInstance, Coroutine> _active = new Dictionary<CardInstance, Coroutine>();
        private readonly Dictionary<CardInstance, Coroutine> _idleShakes = new Dictionary<CardInstance, Coroutine>();

        // ── helpers ──────────────────────────────────────────────────────────

        /// <summary>Wrap Euler angles to -180..180 to avoid lerp spinning.</summary>
        private static Vector3 NormalizeEuler(Vector3 e)
        {
            e.x = (e.x > 180f) ? e.x - 360f : e.x;
            e.y = (e.y > 180f) ? e.y - 360f : e.y;
            e.z = (e.z > 180f) ? e.z - 360f : e.z;
            return e;
        }

        /// <summary>Stop all running card animations and clear tracking.</summary>
        public void StopAll()
        {
            foreach (var kvp in _active)
            {
                if (kvp.Value != null)
                    StopCoroutine(kvp.Value);
            }
            _active.Clear();
        }

        private void StopCard(CardInstance card)
        {
            if (_active.TryGetValue(card, out Coroutine c) && c != null)
                StopCoroutine(c);
        }

        private void Run(CardInstance card, IEnumerator routine)
        {
            StopCard(card);
            _active[card] = StartCoroutine(routine);
        }

        // ── public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Animate card from above the target position down to the target.
        /// </summary>
        public void PlayEntrance(CardInstance card, CardTransformTarget target, float delay)
        {
            Run(card, EntranceRoutine(card, target, delay));
        }

        /// <summary>
        /// Lift card up, scale it, and zero its X-tilt.
        /// </summary>
        public void PlayHoverEnter(CardInstance card, CardTransformTarget arcTarget, System.Action onComplete = null)
        {
            Run(card, HoverEnterRoutine(card, arcTarget, onComplete));
        }

        /// <summary>
        /// Return card to its arc position, rotation (with defaultXTilt), and scale 1.
        /// </summary>
        public void PlayHoverExit(CardInstance card, CardTransformTarget arcTarget)
        {
            Run(card, HoverExitRoutine(card, arcTarget));
        }

        /// <summary>
        /// Move card toward battlefieldTarget then invoke onComplete.
        /// </summary>
        public void PlayExit(CardInstance card, System.Action onComplete)
        {
            Run(card, ExitRoutine(card, onComplete));
        }

        /// <summary>
        /// Shake card horizontally then return it to its current position.
        /// </summary>
        public void PlayRejection(CardInstance card)
        {
            Run(card, RejectionRoutine(card));
        }

        /// <summary>
        /// Scale card up and shake it briefly to show it's selected.
        /// </summary>
        public void PlaySelectPop(CardInstance card)
        {
            Run(card, SelectPopRoutine(card));
        }

        /// <summary>
        /// Lift card up + scale bigger + shake — combined select animation.
        /// </summary>
        public void PlaySelectLift(CardInstance card, CardTransformTarget arcTarget)
        {
            Run(card, SelectLiftRoutine(card, arcTarget));
        }

        // ── coroutines ───────────────────────────────────────────────────────

        private IEnumerator EntranceRoutine(CardInstance card, CardTransformTarget target, float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            if (card == null || card.RectTransform == null) yield break;

            RectTransform rt = card.RectTransform;

            Vector2 startPos = target.anchoredPosition + Vector2.up * startHeightOffset;
            Vector2 endPos   = target.anchoredPosition;

            // Start rotation: same Z as target but no X-tilt yet
            Vector3 startRot = new Vector3(0f, target.rotation.y, target.rotation.z);
            Vector3 endRot   = new Vector3(defaultXTilt, target.rotation.y, target.rotation.z);

            rt.anchoredPosition = startPos;
            rt.localEulerAngles = startRot;

            float elapsed = 0f;
            while (elapsed < entranceDuration)
            {
                if (card == null || rt == null) yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / entranceDuration);
                // Use inspector curve if set, otherwise use built-in slam curve
                AnimationCurve curve = (entranceCurve != null && entranceCurve.length > 0)
                    ? entranceCurve : SlamCurve;
                float curvedT = curve.Evaluate(t);

                rt.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, curvedT);
                rt.localEulerAngles = Vector3.LerpUnclamped(startRot, endRot, t);
                yield return null;
            }

            if (card == null || rt == null) yield break;
            rt.anchoredPosition = endPos;
            rt.localEulerAngles = endRot;
            _active.Remove(card);
        }

        private IEnumerator HoverEnterRoutine(CardInstance card, CardTransformTarget arcTarget, System.Action onComplete = null)
        {
            RectTransform rt = card != null ? card.RectTransform : null;
            if (rt == null) { onComplete?.Invoke(); yield break; }

            Vector2 startPos   = rt.anchoredPosition;
            Vector2 endPos     = new Vector2(arcTarget.anchoredPosition.x,
                                             arcTarget.anchoredPosition.y + hoverLiftHeight);
            Vector3 startRot   = NormalizeEuler(rt.localEulerAngles);
            Vector3 endRot     = Vector3.zero;
            Vector3 startScale = rt.localScale;
            Vector3 endScale   = Vector3.one * hoverScale;

            float elapsed = 0f;
            while (elapsed < hoverDuration)
            {
                if (card == null || rt == null) { onComplete?.Invoke(); yield break; }
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / hoverDuration);

                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                rt.localEulerAngles = Vector3.Lerp(startRot, endRot, t);
                rt.localScale       = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }

            if (card == null || rt == null) { onComplete?.Invoke(); yield break; }
            rt.anchoredPosition = endPos;
            rt.localEulerAngles = endRot;
            rt.localScale       = endScale;
            _active.Remove(card);
            onComplete?.Invoke();
        }

        private IEnumerator HoverExitRoutine(CardInstance card, CardTransformTarget arcTarget)
        {
            RectTransform rt = card != null ? card.RectTransform : null;
            if (rt == null) yield break;

            Vector2 startPos   = rt.anchoredPosition;
            Vector2 endPos     = arcTarget.anchoredPosition;
            Vector3 startRot   = NormalizeEuler(rt.localEulerAngles);
            Vector3 endRot     = new Vector3(defaultXTilt, arcTarget.rotation.y, arcTarget.rotation.z);
            Vector3 startScale = rt.localScale;
            Vector3 endScale   = Vector3.one;

            float elapsed = 0f;
            while (elapsed < hoverDuration)
            {
                if (card == null || rt == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / hoverDuration);

                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                rt.localEulerAngles = Vector3.Lerp(startRot, endRot, t);
                rt.localScale       = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }

            if (card == null || rt == null) yield break;
            rt.anchoredPosition = endPos;
            rt.localEulerAngles = endRot;
            rt.localScale       = endScale;
            _active.Remove(card);
        }

        private IEnumerator ExitRoutine(CardInstance card, System.Action onComplete)
        {
            RectTransform rt = card.RectTransform;

            Vector2 startPos = rt.anchoredPosition;
            Vector2 endPos   = battlefieldTarget;

            float elapsed = 0f;
            while (elapsed < exitDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / exitDuration);

                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }

            rt.anchoredPosition = endPos;
            _active.Remove(card);
            onComplete?.Invoke();
        }

        private IEnumerator RejectionRoutine(CardInstance card)
        {
            RectTransform rt = card.RectTransform;
            Vector2 origin   = rt.anchoredPosition;

            float elapsed = 0f;
            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float t      = elapsed / shakeDuration;
                float decay  = 1f - t;
                float offset = Mathf.Sin(t * Mathf.PI * 8f) * shakeMagnitude * decay;
                rt.anchoredPosition = origin + Vector2.right * offset;
                yield return null;
            }
            rt.anchoredPosition = origin;
            _active.Remove(card);
        }

        private IEnumerator SelectPopRoutine(CardInstance card)
        {
            RectTransform rt = card.RectTransform;
            Vector2 startPos = rt.anchoredPosition;
            Vector2 liftedPos = startPos + Vector2.up * (30f + selectedLiftExtra);
            Vector3 startScale = rt.localScale;
            Vector3 endScale = Vector3.one * selectScale;

            // Quick lift + scale up together
            float elapsed = 0f;
            float popDur = 0.1f;
            while (elapsed < popDur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / popDur);
                rt.anchoredPosition = Vector2.Lerp(startPos, liftedPos, t);
                rt.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }
            rt.anchoredPosition = liftedPos;
            rt.localScale = endScale;

            // Initial burst shake
            Vector2 origin = rt.anchoredPosition;
            elapsed = 0f;
            while (elapsed < selectShakeDur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / selectShakeDur;
                float decay = 1f - t;
                float offset = Mathf.Sin(t * Mathf.PI * 8f) * selectShakeMag * decay;
                rt.anchoredPosition = origin + Vector2.right * offset;
                yield return null;
            }
            rt.anchoredPosition = origin;
            _active.Remove(card);

            // Start continuous idle shake
            StartSelectedIdle(card);
        }

        private IEnumerator SelectLiftRoutine(CardInstance card, CardTransformTarget arcTarget)
        {
            RectTransform rt = card.RectTransform;

            // Phase 1: lift up
            Vector2 startPos   = rt.anchoredPosition;
            Vector2 liftPos    = new Vector2(arcTarget.anchoredPosition.x,
                                             arcTarget.anchoredPosition.y + hoverLiftHeight);
            Vector3 startRot   = rt.localEulerAngles;
            Vector3 endRot     = Vector3.zero;

            float elapsed = 0f;
            while (elapsed < hoverDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / hoverDuration);
                rt.anchoredPosition = Vector2.Lerp(startPos, liftPos, t);
                rt.localEulerAngles = Vector3.Lerp(startRot, endRot, t);
                yield return null;
            }
            rt.anchoredPosition = liftPos;
            rt.localEulerAngles = endRot;

            // Phase 2: scale up
            Vector3 scaleStart = rt.localScale;
            Vector3 scaleEnd   = Vector3.one * selectScale;
            elapsed = 0f;
            float popDur = 0.1f;
            while (elapsed < popDur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / popDur);
                rt.localScale = Vector3.Lerp(scaleStart, scaleEnd, t);
                yield return null;
            }
            rt.localScale = scaleEnd;

            // Phase 3: shake
            Vector2 origin = rt.anchoredPosition;
            elapsed = 0f;
            while (elapsed < selectShakeDur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / selectShakeDur;
                float decay = 1f - t;
                float offset = Mathf.Sin(t * Mathf.PI * 8f) * selectShakeMag * decay;
                rt.anchoredPosition = origin + Vector2.right * offset;
                yield return null;
            }
            rt.anchoredPosition = origin;
            _active.Remove(card);

            // Start continuous idle shake
            StartSelectedIdle(card);
        }

        // ── Selected idle shake ──────────────────────────────────────────────

        /// <summary>Start a subtle continuous shake on a selected card.</summary>
        public void StartSelectedIdle(CardInstance card)
        {
            StopSelectedIdle(card);
            _idleShakes[card] = StartCoroutine(SelectedIdleRoutine(card));
        }

        /// <summary>Stop the idle shake and reset position.</summary>
        public void StopSelectedIdle(CardInstance card)
        {
            if (_idleShakes.TryGetValue(card, out Coroutine c) && c != null)
                StopCoroutine(c);
            _idleShakes.Remove(card);
        }

        private IEnumerator SelectedIdleRoutine(CardInstance card)
        {
            RectTransform rt = card.RectTransform;
            Vector2 origin = rt.anchoredPosition;
            float time = 0f;

            while (card.IsSelected)
            {
                time += Time.deltaTime;
                float xOff = Mathf.Sin(time * selectedIdleSpeed) * selectedIdleShakeMag;
                float yOff = Mathf.Cos(time * selectedIdleSpeed * 0.7f) * selectedIdleShakeMag * 0.5f;
                rt.anchoredPosition = origin + new Vector2(xOff, yOff);
                yield return null;
            }

            rt.anchoredPosition = origin;
            _idleShakes.Remove(card);
        }
    }
}
