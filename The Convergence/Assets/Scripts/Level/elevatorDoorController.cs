using UnityEngine;

public class elevatorDoorController : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private Transform door;
    [SerializeField] private float openHeight = 2f;
    [SerializeField] private float speed = 2f;
    [SerializeField] private bool openDownward = true;
    [SerializeField] private bool closeOnExit = true;

    private Vector3 closedPos;
    private Vector3 openPos;

    private bool doorOpening;
    private bool doorClosing;

    private void Start()
    {
        if (door == null)
        {
            Debug.LogError("Door not assigned to elevatorDoorController!");
            enabled = false;
            return;
        }

        // Store local positions
        closedPos = door.localPosition;

        float offset = openDownward ? -openHeight : openHeight;
        openPos = closedPos + Vector3.up * offset;
    }

    private void Update()
    {
        if (doorOpening)
            MoveDoor(openPos);

        if (doorClosing)
            MoveDoor(closedPos);
    }

    private void MoveDoor(Vector3 targetPos)
    {
        door.localPosition = Vector3.MoveTowards(
            door.localPosition,
            targetPos,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(door.localPosition, targetPos) < 0.01f)
        {
            doorOpening = false;
            doorClosing = false;
        }
    }

    private void OpenDoor()
    {
        doorClosing = false;
        doorOpening = true;
    }

    private void CloseDoor()
    {
        doorOpening = false;
        doorClosing = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        OpenDoor();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (closeOnExit)
            CloseDoor();
    }
}
