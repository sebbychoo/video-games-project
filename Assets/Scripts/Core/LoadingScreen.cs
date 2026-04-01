using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Persistent loading screen with animated dots, icon, and configurable text.
/// Place in your first scene — persists via DontDestroyOnLoad.
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    public static LoadingScreen Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Image blackOverlay;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private Image loadingIcon;

    [Header("Timing")]
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private float holdDuration = 1.2f;
    [SerializeField] private float dotSpeed = 0.4f;

    [Header("Default Text (editable)")]
    [SerializeField] private string elevatorText = "Onto the Next";
    [SerializeField] private string battleText = "Fight!";
    [SerializeField] private string defaultText = "Loading";
    [SerializeField] private bool animateDots = true;

    [Header("Icon Animation")]
    [SerializeField] private Sprite[] iconFrames;
    [SerializeField] private float frameRate = 6f;

    private Canvas _canvas;
    private Coroutine _animCoroutine;
    private Coroutine _dotCoroutine;
    private string _baseText;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _canvas = GetComponent<Canvas>();
        if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 999;

        gameObject.SetActive(true);
        SetAlpha(0f);
        if (loadingText != null) loadingText.alpha = 0f;
        ShowIcon(false);
        if (blackOverlay != null) blackOverlay.raycastTarget = false;
    }

    /// <summary>Load with custom text. Pass null to use default.</summary>
    public void LoadSceneWithFade(string sceneName, string displayText = null)
    {
        StartCoroutine(FadeLoadFade(sceneName, displayText));
    }

    /// <summary>Load for elevator — uses elevatorText + animated dots.</summary>
    public void LoadElevator(string sceneName)
    {
        StartCoroutine(FadeLoadFade(sceneName, elevatorText, true));
    }

    /// <summary>Load for battle — uses battleText, no dots.</summary>
    public void LoadBattle(string sceneName)
    {
        StartCoroutine(FadeLoadFade(sceneName, battleText, false));
    }

    private IEnumerator FadeLoadFade(string sceneName, string text, bool dots = true)
    {
        if (blackOverlay != null) blackOverlay.raycastTarget = true;

        _baseText = string.IsNullOrEmpty(text) ? defaultText : text;
        if (loadingText != null) loadingText.text = _baseText;

        // Fade to black
        yield return StartCoroutine(Fade(0f, 1f));

        // Show text + icon + dots
        if (loadingText != null) loadingText.alpha = 1f;
        ShowIcon(true);
        if (dots && animateDots) StartDots();

        // Load scene async
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (op != null && !op.isDone)
            yield return null;

        // Hold for minimum duration
        yield return new WaitForSecondsRealtime(holdDuration);

        // Hide everything
        StopDots();
        if (loadingText != null) loadingText.alpha = 0f;
        ShowIcon(false);

        // Fade back in
        yield return StartCoroutine(Fade(1f, 0f));

        if (blackOverlay != null) blackOverlay.raycastTarget = false;
    }

    // ── Dot animation ─────────────────────────────────────────

    private void StartDots()
    {
        StopDots();
        _dotCoroutine = StartCoroutine(AnimateDots());
    }

    private void StopDots()
    {
        if (_dotCoroutine != null)
        {
            StopCoroutine(_dotCoroutine);
            _dotCoroutine = null;
        }
    }

    private IEnumerator AnimateDots()
    {
        int dotCount = 0;
        while (true)
        {
            string dots = new string('.', dotCount);
            if (loadingText != null)
                loadingText.text = _baseText + dots;
            dotCount = (dotCount + 1) % 4; // 0, 1, 2, 3, 0, 1...
            yield return new WaitForSecondsRealtime(dotSpeed);
        }
    }

    // ── Fade ──────────────────────────────────────────────────

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            SetAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float a)
    {
        if (blackOverlay != null)
            blackOverlay.color = new Color(0f, 0f, 0f, a);
    }

    // ── Icon animation ────────────────────────────────────────

    private void ShowIcon(bool show)
    {
        if (loadingIcon == null) return;
        loadingIcon.enabled = show;

        if (show && iconFrames != null && iconFrames.Length > 1)
            _animCoroutine = StartCoroutine(AnimateIcon());
        else if (!show && _animCoroutine != null)
        {
            StopCoroutine(_animCoroutine);
            _animCoroutine = null;
        }
    }

    private IEnumerator AnimateIcon()
    {
        int frame = 0;
        float interval = 1f / Mathf.Max(frameRate, 1f);
        while (true)
        {
            if (loadingIcon != null && iconFrames.Length > 0)
            {
                loadingIcon.sprite = iconFrames[frame];
                frame = (frame + 1) % iconFrames.Length;
            }
            yield return new WaitForSecondsRealtime(interval);
        }
    }
}
