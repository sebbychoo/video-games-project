using UnityEngine;
using UnityEngine.UI;

namespace CardBattle
{
    /// <summary>
    /// Displays a dimmed screenshot of the exploration scene as a full-screen
    /// background behind the 2D battle UI. The screenshot is captured by
    /// SceneLoader before the battle scene loads and stored in a static field
    /// so it survives the scene transition.
    /// </summary>
    public class BattleBackground : MonoBehaviour
    {
        /// <summary>
        /// Static texture captured from the exploration scene before loading
        /// the battle scene. Persists across scene loads.
        /// </summary>
        public static Texture2D CapturedBackground { get; private set; }

        [SerializeField] RawImage backgroundImage;
        [SerializeField] Color dimColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        void Start()
        {
            if (backgroundImage == null)
                return;

            if (CapturedBackground != null)
            {
                backgroundImage.texture = CapturedBackground;
                backgroundImage.color = dimColor;
                backgroundImage.enabled = true;
            }
            else
            {
                // No captured background — hide the image so it doesn't
                // render a blank white quad behind the battle UI.
                backgroundImage.enabled = false;
            }
        }

        /// <summary>
        /// Captures the current screen as a Texture2D. Must be called at
        /// end-of-frame (inside a coroutine that yields WaitForEndOfFrame)
        /// so the frame has been fully rendered.
        /// </summary>
        public static void CaptureBackground()
        {
            // Destroy previous capture to avoid leaking GPU memory.
            if (CapturedBackground != null)
                Object.Destroy(CapturedBackground);

            CapturedBackground = ScreenCapture.CaptureScreenshotAsTexture();
        }
    }
}
