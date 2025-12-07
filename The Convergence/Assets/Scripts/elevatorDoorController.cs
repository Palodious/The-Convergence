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
    private bool doorOpening = false;
    private bool doorClosing = false;

    private void Start()
    {
        if (door == null)
        {
            Debug.LogError("Door not assigned to elevatorDoorController!");
            return;
        }

        closedPos = door.localPosition;

        float offset = openDownward ? -openHeight : openHeight;
        openPos = closedPos + new Vector3(0, offset, 0);
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
        door.localPosition = Vector3.MoveTowards(door.localPosition, targetPos, speed * Time.deltaTime);

        if (Vector3.Distance(door.localPosition, targetPos) < 0.01f)
        {
            if (doorOpening) doorOpening = false;
            if (doorClosing) doorClosing = false;
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
        Debug.Log("Trigger entered by: " + other.name);
        if (other.CompareTag("Player"))
            OpenDoor();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && closeOnExit)
        {
            CloseDoor();
        }
    }
}