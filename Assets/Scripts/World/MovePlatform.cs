using UnityEngine;

/// <summary>
/// Moves a platform back and forth along one axis.
/// Choose direction and set speed/distance in the Inspector.
/// </summary>
public class MovePlatform : MonoBehaviour
{
    public enum MoveAxis { LeftRight, BackForth, UpDown }

    [SerializeField] private MoveAxis moveAxis = MoveAxis.LeftRight;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float distance = 2f;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        float delta = Mathf.PingPong(Time.time * speed, distance);

        Vector3 direction = moveAxis switch
        {
            MoveAxis.LeftRight => Vector3.right,
            MoveAxis.BackForth => Vector3.forward,
            MoveAxis.UpDown    => Vector3.up,
            _                  => Vector3.right
        };

        transform.position = startPosition + direction * delta;
    }
}
