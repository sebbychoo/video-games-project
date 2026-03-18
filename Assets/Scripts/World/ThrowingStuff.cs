using UnityEngine;

/// <summary>
/// Throws a prefab forward on left mouse click.
/// Attach to the player or a hand/throw point GameObject.
/// </summary>
public class ThrowingStuff : MonoBehaviour
{
    public GameObject throwablePrefab;
    public float throwForce = 10f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            ThrowObject();
    }

    void ThrowObject()
    {
        GameObject thrown = Instantiate(throwablePrefab, transform.position, transform.rotation);
        Rigidbody rb = thrown.GetComponent<Rigidbody>();
        if (rb != null)
            rb.AddForce(transform.forward * throwForce, ForceMode.Impulse);
    }
}
