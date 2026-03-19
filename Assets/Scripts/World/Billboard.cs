using UnityEngine;

/// <summary>
/// Makes a 2D sprite always face the camera.
/// Attach to any sprite GameObject in the 3D world.
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
        // Face the camera but stay upright (only rotate on Y axis)
        Vector3 dir = _cam.position - transform.position;
        dir.y = 0f;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(-dir);
    }
}
