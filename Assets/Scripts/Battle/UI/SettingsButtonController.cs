using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace CardBattle
{
    /// <summary>
    /// Settings / back button on the battle screen.
    /// Pressing it returns the player to the main menu scene.
    /// Wire OnClick in the Inspector or the button is auto-discovered in Awake.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SettingsButtonController : MonoBehaviour
    {
        private const string MainMenuSceneName = "Menu";

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnSettingsClicked);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnSettingsClicked);
        }

        /// <summary>Returns to the main menu scene.</summary>
        public void OnSettingsClicked()
        {
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadSceneMenu(MainMenuSceneName);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                SceneManager.LoadScene(MainMenuSceneName);
            }
        }
    }
}
