using UnityEngine;

/// <summary>
/// Spawns a prefab at a target location when a tagged object enters this trigger.
/// Set collider to Is Trigger. Assign prefab, spawn point, and tag in Inspector.
/// </summary>
public class OnTriggerSpawn : MonoBehaviour
{
    public GameObject prefabToSpawn;
    public GameObject spawnPoint;
    public string triggerTag;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(triggerTag))
            Instantiate(prefabToSpawn, spawnPoint.transform.position, spawnPoint.transform.rotation);
    }
}
