using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CardBattle;

/// <summary>
/// Persistent HP bar for the exploration scene.
/// Reads HP from RunState (not the Health component, which resets on Awake).
/// Also listens to WaterCooler heals and any direct RunState writes via the
/// static event ExplorationHPBar.OnRunStateHPChanged.
/// </summary>
public class ExplorationHPBar : MonoBehaviour
{
    [Header("Fill")]
    [SerializeField] Image hpBarFill;

    [Header("Text")]
    [SerializeField] TextMeshProUGUI hpText;

    [Header("Low-HP Pulse")]
    [SerializeField] GameObject criticalRoot;
    [SerializeField] float criticalThreshold = 0.25f;

    [Header("Lerp")]
    [SerializeField] float lerpSpeed = 6f;

    // ── Static event so any system (WaterCooler, shops, etc.) can notify us ──
    /// <summary>Raise this after writing to RunState.playerHP to refresh the bar instantly.</summary>
    public static event System.Action OnRunStateHPChanged;

    /// <summary>Convenience helper any script can call instead of raising the event directly.</summary>
    public static void NotifyHPChanged() => OnRunStateHPChanged?.Invoke();

    // ── Private ───────────────────────────────────────────────────────────────
    private float _targetFill;
    private float _currentFill = 1f;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void OnEnable()
    {
        OnRunStateHPChanged += Refresh;
    }

    private void OnDisable()
    {
        OnRunStateHPChanged -= Refresh;
    }

    private void Start()
    {
        Refresh();
    }

    private void Update()
    {
        if (Mathf.Approximately(_currentFill, _targetFill)) return;
        _currentFill = Mathf.Lerp(_currentFill, _targetFill, Time.deltaTime * lerpSpeed);
        ApplyFill(_currentFill);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Re-reads RunState and refreshes all visuals. Call after any HP change.</summary>
    public void Refresh()
    {
        RunState run = GetRunState();
        if (run == null || run.playerMaxHP <= 0 || run.playerHP <= 0)
        {
            // No active run or HP not yet written — hide rather than show wrong values
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        int current = Mathf.Max(run.playerHP, 0);
        int max     = run.playerMaxHP;
        SetValues(current, max);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetValues(int current, int max)
    {
        _targetFill = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;

        if (hpText != null)
            hpText.text = $"{current}<size=70%>/{max}</size>";

        if (criticalRoot != null)
            criticalRoot.SetActive(max > 0 && current > 0 && (float)current / max <= criticalThreshold);
    }

    private void ApplyFill(float fill)
    {
        if (hpBarFill != null)
            hpBarFill.fillAmount = fill;
    }

    private static RunState GetRunState()
    {
        return SaveManager.Instance != null ? SaveManager.Instance.CurrentRun : null;
    }
}
