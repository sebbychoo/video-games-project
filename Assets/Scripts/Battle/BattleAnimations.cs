using System.Collections;
using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Handles 3D battle animations: hit shake, attack dash, death fall.
    /// Attach to the BattleManager GameObject.
    /// </summary>
    public class BattleAnimations : MonoBehaviour
    {
        public static BattleAnimations Instance { get; private set; }

        [Header("Hit Shake")]
        [SerializeField] float shakeDuration  = 0.3f;
        [SerializeField] float shakeMagnitude = 0.15f;

        [Header("Attack Dash")]
        [SerializeField] float dashDistance = 1.5f;
        [SerializeField] float dashDuration = 0.25f;

        [Header("Death Animation")]
        [SerializeField] float deathTipDuration = 0.6f;
        [SerializeField] float deathSinkDuration = 0.8f;
        [SerializeField] float deathSinkDepth = 3f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Shake a target when it gets hit. Freezes rigidbody during shake.</summary>
        public void PlayHitShake(Transform target)
        {
            if (target != null)
                StartCoroutine(HitShakeRoutine(target));
        }

        /// <summary>Dash the attacker toward the target then back.</summary>
        public void PlayAttackDash(Transform attacker, Transform target, System.Action onComplete = null)
        {
            if (attacker != null && target != null)
                StartCoroutine(AttackDashRoutine(attacker, target, onComplete));
            else
                onComplete?.Invoke();
        }

        /// <summary>Target tips backward and sinks into the ground.</summary>
        public void PlayDeath(Transform target, System.Action onComplete = null)
        {
            if (target != null)
                StartCoroutine(DeathRoutine(target, onComplete));
            else
                onComplete?.Invoke();
        }

        private IEnumerator HitShakeRoutine(Transform target)
        {
            Vector3 origin = target.localPosition;
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float decay = 1f - (elapsed / shakeDuration);
                float offsetX = Random.Range(-1f, 1f) * shakeMagnitude * decay;
                float offsetZ = Random.Range(-1f, 1f) * shakeMagnitude * decay;

                target.localPosition = origin + new Vector3(offsetX, 0f, offsetZ);
                yield return null;
            }

            target.localPosition = origin;
        }

        private IEnumerator AttackDashRoutine(Transform attacker, Transform target, System.Action onComplete)
        {
            Vector3 startPos = attacker.localPosition;
            Vector3 worldDir = (target.position - attacker.position).normalized;
            Vector3 localDashOffset = attacker.parent != null
                ? attacker.parent.InverseTransformDirection(worldDir * dashDistance)
                : worldDir * dashDistance;
            Vector3 dashPos = startPos + localDashOffset;

            // Dash forward
            float elapsed = 0f;
            float halfDuration = dashDuration * 0.5f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                t = 1f - (1f - t) * (1f - t); // ease out
                attacker.localPosition = Vector3.Lerp(startPos, dashPos, t);
                yield return null;
            }

            yield return new WaitForSeconds(0.05f);

            // Dash back
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                attacker.localPosition = Vector3.Lerp(dashPos, startPos, t);
                yield return null;
            }

            attacker.localPosition = startPos;

            onComplete?.Invoke();
        }

        private IEnumerator DeathRoutine(Transform target, System.Action onComplete)
        {
            // Disable physics so it doesn't interfere
            Rigidbody rb = target.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            Collider col = target.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Vector3 startRot = target.localEulerAngles;
            // Tip backward (rotate on X axis)
            Vector3 endRot = startRot + new Vector3(-90f, 0f, 0f);

            // Phase 1: tip backward
            float elapsed = 0f;
            while (elapsed < deathTipDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / deathTipDuration);
                // Ease in — slow start, fast finish like falling
                t = t * t;
                target.localEulerAngles = Vector3.Lerp(startRot, endRot, t);
                yield return null;
            }
            target.localEulerAngles = endRot;

            // Phase 2: sink into the ground
            Vector3 startPos = target.localPosition;
            Vector3 endPos = startPos + Vector3.down * deathSinkDepth;

            elapsed = 0f;
            while (elapsed < deathSinkDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / deathSinkDuration);
                // Ease in — accelerate downward
                t = t * t;
                target.localPosition = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }
            target.localPosition = endPos;

            onComplete?.Invoke();
        }
    }
}
