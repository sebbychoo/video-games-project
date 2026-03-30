using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using StarterAssets;
#endif

namespace CardBattle
{
    /// <summary>
    /// Renders two 2D pixel art hand sprites overlaid on the camera view
    /// (bottom-left and bottom-right) during exploration.
    ///
    /// Animations:
    ///   - Idle breathing: subtle vertical sine wave when not moving
    ///   - Walk bob: vertical sine wave synced to movement speed
    ///   - Sprint pump: faster, larger vertical bob while sprinting
    ///   - Interact: one hand reaches forward (scale up + move toward center)
    ///
    /// Requirements: 18.1 – 18.8
    /// </summary>
    public class FirstPersonHandsController : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Hand Sprites")]
        [Tooltip("SpriteRenderer for the left hand (bottom-left of screen).")]
        public SpriteRenderer leftHand;
        [Tooltip("SpriteRenderer for the right hand (bottom-right of screen).")]
        public SpriteRenderer rightHand;

        [Header("Player References")]
        [Tooltip("The player GameObject (tagged 'Player'). Auto-found if null.")]
        public GameObject playerRoot;

        [Tooltip("The camera to parent hands to. Auto-found via Camera.main if null.")]
        public Camera handsCamera;

        [Header("Positioning")]
        [Tooltip("Base local position of the left hand relative to the camera overlay anchor.")]
        public Vector3 leftHandBasePos = new Vector3(-0.35f, -0.28f, 0.5f);
        [Tooltip("Base local position of the right hand relative to the camera overlay anchor.")]
        public Vector3 rightHandBasePos = new Vector3(0.35f, -0.28f, 0.5f);

        [Header("Idle Breathing")]
        public float breathAmplitude = 0.008f;
        public float breathFrequency = 0.8f;

        [Header("Walk Bob")]
        public float walkBobAmplitude = 0.025f;
        public float walkBobFrequency = 2.2f;

        [Header("Sprint Bob")]
        public float sprintBobAmplitude = 0.055f;
        public float sprintBobFrequency = 4.0f;

        [Header("Interact Animation")]
        [Tooltip("How far the interacting hand moves forward (toward screen center).")]
        public float interactReachDistance = 0.12f;
        public float interactDuration = 0.25f;

        [Header("Speed Thresholds")]
        public float walkSpeedThreshold = 0.1f;
        public float sprintSpeedThreshold = 5.5f;

        // ── Private state ──────────────────────────────────────────────────────

        private Camera _mainCamera;
        private CharacterController _characterController;
#if ENABLE_INPUT_SYSTEM
        private StarterAssetsInputs _input;
#endif

        private float _bobTimer;
        private bool _isInteracting;
        private bool _hidden;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            _mainCamera = handsCamera != null ? handsCamera : Camera.main;

            // Parent both hands to the target camera so they follow it exactly.
            if (_mainCamera != null)
            {
                if (leftHand != null)  leftHand.transform.SetParent(_mainCamera.transform, false);
                if (rightHand != null) rightHand.transform.SetParent(_mainCamera.transform, false);
            }

            ResetHandPositions();
        }

        private void Start()
        {
            if (playerRoot == null)
                playerRoot = GameObject.FindWithTag("Player");

            if (playerRoot != null)
            {
                _characterController = playerRoot.GetComponent<CharacterController>();
#if ENABLE_INPUT_SYSTEM
                _input = playerRoot.GetComponent<StarterAssetsInputs>();
#endif
            }
        }

        private void LateUpdate()
        {
            if (_hidden || _isInteracting) return;

            float speed = GetHorizontalSpeed();
            bool sprinting = IsSprinting();

            if (speed > walkSpeedThreshold)
            {
                // Walk or sprint bob
                float amp  = sprinting ? sprintBobAmplitude  : walkBobAmplitude;
                float freq = sprinting ? sprintBobFrequency  : walkBobFrequency;
                _bobTimer += Time.deltaTime * freq * Mathf.PI * 2f;
                float bob = Mathf.Sin(_bobTimer) * amp;
                ApplyVerticalOffset(bob);
            }
            else
            {
                // Idle breathing
                float breath = Mathf.Sin(Time.time * breathFrequency * Mathf.PI * 2f) * breathAmplitude;
                ApplyVerticalOffset(breath);
                _bobTimer = 0f; // reset so bob starts cleanly on next move
            }
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Play the reach-forward interaction animation on the right hand,
        /// then return to idle. Call this when the player presses the interact key.
        /// </summary>
        public void PlayInteraction()
        {
            if (_hidden || _isInteracting) return;
            StartCoroutine(InteractCoroutine());
        }

        /// <summary>
        /// Hide both hand sprites immediately (call before battle scene transition).
        /// Requirement 18.8
        /// </summary>
        public void HideHands()
        {
            _hidden = true;
            SetHandsVisible(false);
        }

        /// <summary>
        /// Show both hand sprites (call when returning to exploration).
        /// </summary>
        public void ShowHands()
        {
            _hidden = false;
            SetHandsVisible(true);
            ResetHandPositions();
        }

        /// <summary>
        /// Set the hand state explicitly (used by design.md SetState(HandState) API).
        /// </summary>
        public void SetState(HandState state)
        {
            switch (state)
            {
                case HandState.Hidden:
                    HideHands();
                    break;
                case HandState.Visible:
                    ShowHands();
                    break;
                case HandState.Interacting:
                    PlayInteraction();
                    break;
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private float GetHorizontalSpeed()
        {
            if (_characterController == null) return 0f;
            Vector3 hVel = _characterController.velocity;
            hVel.y = 0f;
            return hVel.magnitude;
        }

        private bool IsSprinting()
        {
#if ENABLE_INPUT_SYSTEM
            if (_input != null) return _input.sprint;
#endif
            return Input.GetKey(KeyCode.LeftShift);
        }

        private void ApplyVerticalOffset(float yOffset)
        {
            if (leftHand != null)
            {
                Vector3 p = leftHandBasePos;
                p.y += yOffset;
                leftHand.transform.localPosition = p;
            }
            if (rightHand != null)
            {
                Vector3 p = rightHandBasePos;
                p.y += yOffset;
                rightHand.transform.localPosition = p;
            }
        }

        private void ResetHandPositions()
        {
            if (leftHand != null)  leftHand.transform.localPosition  = leftHandBasePos;
            if (rightHand != null) rightHand.transform.localPosition = rightHandBasePos;
        }

        private void SetHandsVisible(bool visible)
        {
            if (leftHand != null)  leftHand.enabled  = visible;
            if (rightHand != null) rightHand.enabled = visible;
        }

        private IEnumerator InteractCoroutine()
        {
            _isInteracting = true;

            // Reach forward: move right hand toward screen center and scale up slightly
            Vector3 startPos   = rightHandBasePos;
            Vector3 targetPos  = rightHandBasePos + new Vector3(-interactReachDistance * 0.5f, interactReachDistance, 0f);
            Vector3 startScale = rightHand != null ? rightHand.transform.localScale : Vector3.one;
            Vector3 targetScale = startScale * 1.15f;

            float elapsed = 0f;
            float half = interactDuration * 0.5f;

            // Phase 1: reach out
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / half);
                if (rightHand != null)
                {
                    rightHand.transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
                    rightHand.transform.localScale    = Vector3.Lerp(startScale, targetScale, t);
                }
                yield return null;
            }

            // Phase 2: return to idle
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / half);
                if (rightHand != null)
                {
                    rightHand.transform.localPosition = Vector3.Lerp(targetPos, startPos, t);
                    rightHand.transform.localScale    = Vector3.Lerp(targetScale, startScale, t);
                }
                yield return null;
            }

            // Snap to base
            if (rightHand != null)
            {
                rightHand.transform.localPosition = startPos;
                rightHand.transform.localScale    = startScale;
            }

            _isInteracting = false;
        }
    }

    /// <summary>Hand display states (matches design.md SetState API).</summary>
    public enum HandState
    {
        Visible,
        Hidden,
        Interacting
    }
}
