using System;
using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Lightweight sprite-frame animation player. Cycles through a SpriteFrameAnimation's
    /// frames at the configured frame rate on a SpriteRenderer.
    /// </summary>
    public class SpriteFrameAnimator : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private SpriteFrameAnimation _currentAnim;
        private Action _onComplete;
        private bool _loop;
        private float _timer;
        private int _frameIndex;

        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Play a sprite-frame animation on this object's SpriteRenderer.
        /// </summary>
        /// <param name="anim">The animation data to play.</param>
        /// <param name="loop">Whether to loop the animation.</param>
        /// <param name="onComplete">Called when a non-looping animation finishes its last frame.</param>
        public void Play(SpriteFrameAnimation anim, bool loop = true, Action onComplete = null)
        {
            if (anim == null || anim.frames == null || anim.frames.Length == 0)
            {
                Debug.LogWarning("[SpriteFrameAnimator] Animation has no frames — skipping.");
                onComplete?.Invoke();
                return;
            }

            if (_renderer == null)
                _renderer = GetComponent<SpriteRenderer>();

            _currentAnim = anim;
            _loop = loop;
            _onComplete = onComplete;
            _frameIndex = 0;
            _timer = 0f;
            IsPlaying = true;

            _renderer.sprite = _currentAnim.frames[0];
        }

        /// <summary>
        /// Stop the current animation immediately.
        /// </summary>
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

            if (_renderer != null)
                _renderer.sprite = _currentAnim.frames[_frameIndex];
        }
    }
}
