using UnityEngine;
using CardBattle;

public class InteractKey : MonoBehaviour
{
    private FirstPersonHandsController _hands;

    private void Start()
    {
        _hands = FindFirstObjectByType<FirstPersonHandsController>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("Interaction key pressed.");
            if (_hands != null) _hands.PlayInteraction();
        }
    }
}
