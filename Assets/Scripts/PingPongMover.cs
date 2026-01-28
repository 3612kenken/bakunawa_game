using UnityEngine;

/// <summary>
/// Moves an object back and forth (ping-pong) on X, Y, or both axes.
/// Useful for moving platforms, traps, hazards, decorations.
/// </summary>
public class PingPongMover : MonoBehaviour
{
    public enum MoveAxis { X, Y, XY }

    [Header("Movement Axis")]
    public MoveAxis axis = MoveAxis.X;

    [Header("Movement Settings")]
    public float distance = 3f;     // How far it moves from start point
    public float speed = 2f;        // Movement speed
    public bool useLocalPosition = false;

    [Header("Advanced")]
    public bool startRandomOffset = false;

    private Vector3 startPos;
    private float timeOffset;

    private void Start()
    {
        startPos = useLocalPosition ? transform.localPosition : transform.position;

        if (startRandomOffset)
            timeOffset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        float t = Mathf.PingPong(Time.time * speed + timeOffset, distance);

        Vector3 offset = Vector3.zero;

        switch (axis)
        {
            case MoveAxis.X:
                offset = new Vector3(t, 0f, 0f);
                break;

            case MoveAxis.Y:
                offset = new Vector3(0f, t, 0f);
                break;

            case MoveAxis.XY:
                offset = new Vector3(t, t, 0f);
                break;
        }

        if (useLocalPosition)
            transform.localPosition = startPos + offset;
        else
            transform.position = startPos + offset;
    }
}
