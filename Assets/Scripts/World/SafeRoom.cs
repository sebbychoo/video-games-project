using UnityEngine;

/// <summary>
/// Marker component placed on bathroom and break room GameObjects.
/// EnemyFollow checks for this component to prevent enemies from entering
/// or chasing the player into these rooms.
/// </summary>
public class SafeRoom : MonoBehaviour
{
    [Tooltip("Collider that defines the safe room boundary. Auto-detected if not assigned.")]
    public Collider roomBounds;

    private void Awake()
    {
        if (roomBounds == null)
            roomBounds = GetComponent<Collider>();
    }

    /// <summary>Returns true if the given world position is inside this safe room.</summary>
    public bool Contains(Vector3 worldPos)
    {
        if (roomBounds == null) return false;
        return roomBounds.bounds.Contains(worldPos);
    }
}
