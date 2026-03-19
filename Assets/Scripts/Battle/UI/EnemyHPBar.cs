using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Simple enemy HP bar. Attach to a UI panel above the enemy.
    /// Works with both Filled images (fillAmount) and regular images (scales X).
    /// </summary>
    public class EnemyHPBar : MonoBehaviour
    {
        [SerializeField] Image fillImage;
        [SerializeField] TextMeshProUGUI hpText;
        [SerializeField] float lerpSpeed = 8f;

        private float _targetFill;
        private float _currentFill;
        private bool _useFillAmount;

        public void Initialize(int currentHP, int maxHP)
        {
            _targetFill = (float)currentHP / maxHP;
            _currentFill = _targetFill;

            if (fillImage != null)
            {
                // Check if image is set to Filled type
                _useFillAmount = (fillImage.type == Image.Type.Filled);

                if (_useFillAmount)
                    fillImage.fillAmount = _currentFill;
                else
                    fillImage.rectTransform.localScale = new Vector3(_currentFill, 1f, 1f);
            }

            UpdateText(currentHP, maxHP);
        }

        public void UpdateHP(int currentHP, int maxHP)
        {
            _targetFill = Mathf.Clamp01((float)currentHP / maxHP);
            UpdateText(currentHP, maxHP);
        }

        private void Update()
        {
            if (fillImage == null) return;

            _currentFill = Mathf.Lerp(_currentFill, _targetFill, Time.deltaTime * lerpSpeed);

            if (_useFillAmount)
                fillImage.fillAmount = _currentFill;
            else
                fillImage.rectTransform.localScale = new Vector3(_currentFill, 1f, 1f);
        }

        private void UpdateText(int current, int max)
        {
            if (hpText != null)
                hpText.text = $"{current} / {max}";
        }
    }
}
