using System;
using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Lightweight sprite-frame animation player. Cycles through a SpriteFrameAnimation's
    /// frames at the configured frame rate. Supports both SpriteRenderer and MeshRenderer
    /// (swaps material texture for mesh-based enemies).
    /// </summary>
    public class SpriteFrameAnimator : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;
        private MeshRenderer _meshRenderer;
        private SpriteFrameAnimation _currentAnim;
        private Action _onComplete;
        private bool _loop;
        private float _timer;
        private int _frameIndex;

        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            FindRenderers();
        }

        private void FindRenderers()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_meshRenderer == null)
                _meshRenderer = GetComponent<MeshRenderer>();
        }

        public void Play(SpriteFrameAnimation anim, bool loop = true, Action onComplete = null)
        {
            if (anim == null || anim.frames == null || anim.frames.Length == 0)
            {
                Debug.LogWarning("[SpriteFrameAnimator] Animation has no frames — skipping.");
                onComplete?.Invoke();
                return;
            }

            FindRenderers();

            _currentAnim = anim;
            _loop = loop;
            _onComplete = onComplete;
            _frameIndex = 0;
            _timer = 0f;
            IsPlaying = true;

            ApplyFrame(0);
        }

        public void Stop()
        {
            IsPlaying = false;
            _currentAnim = null;
            _onComplete = null;
        }

        private void Update()
        {
            if (!IsPlaying || _currentAnim == null)
                return;

            float interval = 1f / Mathf.Max(_currentAnim.frameRate, 0.001f);
            _timer += Time.deltaTime;

            if (_timer < interval)
                return;

            _timer -= interval;
            _frameIndex++;

            if (_frameIndex >= _currentAnim.frames.Length)
            {
                if (_loop)
                {
                    _frameIndex = 0;
                }
                else
                {
                    IsPlaying = false;
                    var callback = _onComplete;
                    _onComplete = null;
                    callback?.Invoke();
                    return;
                }
            }

            ApplyFrame(_frameIndex);
        }

        private void ApplyFrame(int index)
        {
            Sprite frame = _currentAnim.frames[index];
            if (frame == null) return;

            // Prefer SpriteRenderer if available
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = frame;
                return;
            }

            // Fall back to MeshRenderer — swap the main texture
            if (_meshRenderer != null && _meshRenderer.material != null)
            {
                _meshRenderer.material.mainTexture = frame.texture;
            }
        }
    }
}
