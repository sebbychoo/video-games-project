using UnityEngine;

/// <summary>
/// Handles camera FOV correction so the game looks consistent
/// across different resolutions and aspect ratios (1080p, 4K, ultrawide, etc.)
/// Attach this to your Camera GameObject.
/// </summary>
public class CameraManager : MonoBehaviour
{
    [Header("Reference Aspect Ratio (16:9)")]
    private float referenceAspect = 16f / 9f;

    [Header("Exploration (3D) Settings")]
    public float explorationFOV = 60f;

    [Header("Battle (2.5D) Settings")]
    public float battleFOV = 45f;
    public bool isBattleScene = false;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        ApplyCameraSettings();
    }

    /// <summary>
    /// Call this when switching between exploration and battle scenes.
    /// </summary>
    public void SetBattleMode(bool inBattle)
    {
        isBattleScene = inBattle;
        ApplyCameraSettings();
    }

    private void ApplyCameraSettings()
    {
        float targetFOV = isBattleScene ? battleFOV : explorationFOV;
        cam.fieldOfView = AdjustFOVForAspect(targetFOV);
    }

    /// <summary>
    /// Adjusts FOV so it matches the intended look on 16:9,
    /// and compensates on narrower or wider screens.
    /// </summary>
    private float AdjustFOVForAspect(float baseFOV)
    {
        float currentAspect = (float)Screen.width / Screen.height;

        // If aspect matches reference, no adjustment needed
        if (Mathf.Approximately(currentAspect, referenceAspect))
            return baseFOV;

        float baseRadians = baseFOV * 0.5f * Mathf.Deg2Rad;
        float adjustedFOV = 2f * Mathf.Atan(Mathf.Tan(baseRadians) * referenceAspect / currentAspect) * Mathf.Rad2Deg;

        return adjustedFOV;
    }
}
