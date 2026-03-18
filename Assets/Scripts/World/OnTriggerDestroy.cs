using UnityEngine;

/// <summary>
/// Destroys a target GameObject when a tagged object enters this trigger.
/// Set collider to Is Trigger. Assign target and tag in Inspector.
/// </summary>
public class OnTriggerDestroy : MonoBehaviour
{
    public GameObject objectToDestroy;
    public string triggerTag;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(triggerTag))
            Destroy(objectToDestroy);
    }
}
