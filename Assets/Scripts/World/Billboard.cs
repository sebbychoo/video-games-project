using UnityEngine;

/// <summary>
/// Simple billboard — makes any object always face the camera.
/// Use SpriteEntity instead for sprites (it includes this + more).
/// This is for non-sprite objects like particle effects or UI in world space.
/// </summary>
public class Billboard : MonoBehaviour
{
    private Transform _cam;

    private void Start()
    {
        _cam = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (_cam == null) return;
        Vector3 dir = _cam.position - transform.position;
        dir.y = 0f;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(-dir);
    }
}
