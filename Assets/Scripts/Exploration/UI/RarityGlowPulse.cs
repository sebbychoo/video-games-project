using UnityEngine;
using UnityEngine.UI;

namespace CardBattle
{
    /// <summary>
    /// Pulses the alpha of an Image between min and max to create a glow effect.
    /// Uses unscaled time so it works while Time.timeScale is 0.
    /// </summary>
    public class RarityGlowPulse : MonoBehaviour
    {
        private Image _image;
        private Color _baseColor;
        private float _minAlpha = 0.4f;
        private float _maxAlpha = 1f;
        private float _speed = 2f;

        public void Initialize(Color color, float speed = 2f)
        {
            _image = GetComponent<Image>();
            _baseColor = color;
            _speed = speed;
        }

        private void Update()
        {
            if (_image == null) return;

            float t = (Mathf.Sin(Time.unscaledTime * _speed) + 1f) / 2f;
            float alpha = Mathf.Lerp(_minAlpha, _maxAlpha, t);
            _image.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, alpha);
        }
    }
}
