using UnityEngine;

public class InteractKey : MonoBehaviour
{
    void Update ()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("Interaction key pressed.");
        }
    }
}
