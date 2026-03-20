using UnityEngine;

/// <summary>
/// Directional sprite entity for 2D-in-3D (DOOM style).
/// Swaps between front/back sprites based on camera angle.
/// Always faces camera on Y axis so the flat sprite is visible.
///
/// Setup:
/// 1. Add SpriteRenderer + this script to your GameObject
/// 2. Assign frontSprite (required), backSprite (optional — uses front as fallback)
/// 3. Later: add left/right sprites for 4-direction or 8-direction
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteEntity : MonoBehaviour
{
    [Header("Directional Sprites")]
    [SerializeField] Sprite frontSprite;
    [SerializeField] Sprite backSprite;

    [Header("Settings")]
    [SerializeField] bool lockUpright = true;

    private Transform _cam;
    private SpriteRenderer _sr;
    private Vector3 _lastPos;
    private Vector3 _moveDir;

    private void Start()
    {
        _cam = Camera.main.transform;
        _sr = GetComponent<SpriteRenderer>();
        _lastPos = transform.position;

        // Pixel art should not be filtered
        if (_sr.sprite != null && _sr.sprite.texture != null)
            _sr.sprite.texture.filterMode = FilterMode.Point;
    }

    private void LateUpdate()
    {
        if (_cam == null) return;

        // Track movement direction
        Vector3 delta = transform.position - _lastPos;
        delta.y = 0f;
        if (delta.sqrMagnitude > 0.0001f)
            _moveDir = delta.normalized;
        _lastPos = transform.position;

        // Billboard — face camera on Y axis
        Vector3 toCamera = _cam.position - transform.position;
        if (lockUpright) toCamera.y = 0f;
        if (toCamera != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(-toCamera);

        // Pick sprite based on whether entity is moving toward or away from camera
        UpdateDirectionalSprite(toCamera);
    }

    private void UpdateDirectionalSprite(Vector3 toCamera)
    {
        if (frontSprite == null) return;

        // Dot between movement direction and direction to camera
        // Positive = moving toward camera (show front), Negative = moving away (show back)
        float dot = _moveDir.sqrMagnitude > 0.001f
            ? Vector3.Dot(_moveDir, toCamera.normalized)
            : 1f; // default to front if not moving

        Sprite target;
        if (dot >= 0f)
            target = frontSprite;
        else
            target = backSprite != null ? backSprite : frontSprite;

        if (_sr.sprite != target)
        {
            _sr.sprite = target;
            if (target != null && target.texture != null)
                target.texture.filterMode = FilterMode.Point;
        }
    }
}
