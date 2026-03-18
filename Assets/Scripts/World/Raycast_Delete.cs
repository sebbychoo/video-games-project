using UnityEngine;

/// <summary>
/// Deletes a tagged object when the player clicks on it.
/// Attach to the Camera.
/// </summary>
public class Raycast_Delete : MonoBehaviour
{
    public string targetTag;

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.CompareTag(targetTag))
                Destroy(hit.collider.gameObject);
        }
    }
}
