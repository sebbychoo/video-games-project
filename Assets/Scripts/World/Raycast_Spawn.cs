using UnityEngine;

/// <summary>
/// Spawns a prefab at the point the player clicks in the world.
/// Attach to the Camera.
/// </summary>
public class Raycast_Spawn : MonoBehaviour
{
    public GameObject prefabToSpawn;

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
            Instantiate(prefabToSpawn, hit.point, prefabToSpawn.transform.rotation);
    }
}
