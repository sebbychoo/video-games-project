using UnityEngine;

/// <summary>
/// Attach this to the SPRITE CHILD of the enemy prefab (not the root).
/// The root's rotation is controlled by NavMeshAgent (enemy body direction).
/// This script rotates only the sprite to face the camera and picks the
/// correct sprite based on the ROOT's facing direction.
/// </summary>
public class EnemyBillboard : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite frontSprite;
    public Sprite backSprite;
    public Sprite sideSprite;

    [Header("References")]
    public SpriteRenderer spriteRenderer;

    private Transform _cam;
    private Transform _root; // the enemy root (parent of this sprite child)

    private void Start()
    {
        _cam = Camera.main?.transform;
        _root = transform.parent != null ? transform.parent : transform;
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (_cam == null || spriteRenderer == null) return;

        // Rotate THIS sprite to always face the camera (billboard)
        Vector3 dirToCamera = _cam.position - transform.position;
        transform.rotation = Quaternion.LookRotation(-dirToCamera);

        // Use the ROOT's forward (NavMeshAgent controls this) for sprite selection
        Vector3 rootForward = new Vector3(_root.forward.x, 0, _root.forward.z).normalized;
        Vector3 toCameraFlat = new Vector3(dirToCamera.x, 0, dirToCamera.z).normalized;

        float dot  = Vector3.Dot(rootForward, toCameraFlat);
        float side = Vector3.Dot(_root.right, toCameraFlat);

        // dot < 0: root forward points away from camera → show back sprite
        // dot > 0: root forward points toward camera → show front sprite
        if (dot > 0.5f)
        {
            spriteRenderer.sprite = frontSprite;
            spriteRenderer.flipX = false;
        }
        else if (dot < -0.5f)
        {
            spriteRenderer.sprite = backSprite != null ? backSprite : frontSprite;
            spriteRenderer.flipX = false;
        }
        else
        {
            spriteRenderer.sprite = sideSprite != null ? sideSprite : frontSprite;
            spriteRenderer.flipX = side < 0;
        }
    }
}
