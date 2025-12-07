using UnityEngine;

public class elevator : MonoBehaviour
{
    [Header("Elevator Settings")]
    [SerializeField] float acceleration = 1.2f;
    [SerializeField] float maxSpeed = 3.5f;
    [SerializeField] float deceleration = 1.5f;
    [SerializeField] float positionSnapThreshold = 0.01f;

    [Header("Target Floor")]
    [SerializeField] Transform targetPoint;

    [Header("Door Settings")]
    [SerializeField] Transform door;
    [SerializeField] float doorOpenHeight = 2f;
    [SerializeField] float doorSpeed = 2f;

    private Vector3 doorClosedPos;
    private Vector3 doorOpenPos;

    private bool doorOpening = false;
    private bool doorClosing = false;

    private float currentSpeed = 0f;
    private float travelDistance = 0f;
    private float travelProgress = 0f;

    private bool isMoving = false;
    private bool elevatorUsed = false;
    private bool playerOnElevator = false;

    private Transform player;
    

    // New: Track elevator state
    private Vector3 currentTargetPosition;
    private bool isAtTarget = false;

    private void Start()
    {
        doorClosedPos = door.localPosition;
        doorOpenPos = doorClosedPos + new Vector3(0, -doorOpenHeight, 0);

        CalculateTravelDistance();
    }

    private void Update()
    {
        if (isMoving)
            MoveElevatorWithLerp();

        if (doorOpening)
            MoveDoorOpen();

        if (doorClosing)
            MoveDoorClosed();
    }

    private void MoveDoorOpen()
    {
        door.localPosition = Vector3.MoveTowards(door.localPosition, doorOpenPos, doorSpeed * Time.deltaTime);
        if (Vector3.Distance(door.localPosition, doorOpenPos) < 0.01f)
            doorOpening = false;
    }

    private void MoveDoorClosed()
    {
        door.localPosition = Vector3.MoveTowards(door.localPosition, doorClosedPos, doorSpeed * Time.deltaTime);
        if (Vector3.Distance(door.localPosition, doorClosedPos) < 0.01f)
            doorClosing = false;
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

    private void CalculateTravelDistance()
    {
        travelDistance = Vector3.Distance(transform.position, targetPoint.position);
    }

    public void StartElevator()
    {
        if (elevatorUsed) return;

        elevatorUsed = true;
        travelProgress = 0f;
        currentSpeed = 0f;
        isMoving = true;
        isAtTarget = false;

        CalculateTravelDistance();
        CloseDoor();
    }

    // FIXED: Using lerp for smooth movement with proper constraints
    private void MoveElevatorWithLerp()
    {
        if (travelDistance <= 0) return;

        float dt = Time.deltaTime;

        // Calculate stopping distance
        float stoppingDistance = (currentSpeed * currentSpeed) / (2f * deceleration);

        // Calculate remaining distance
        float remainingDistance = travelDistance - (travelProgress * travelDistance);

        // Acceleration/Deceleration logic
        if (remainingDistance <= stoppingDistance && currentSpeed > 0)
        {
            // Decelerate
            currentSpeed = Mathf.Max(0, currentSpeed - deceleration * dt);
        }
        else if (currentSpeed < maxSpeed)
        {
            // Accelerate
            currentSpeed = Mathf.Min(maxSpeed, currentSpeed + acceleration * dt);
        }

        // Calculate movement for this frame
        float moveDistance = currentSpeed * dt;
        travelProgress += moveDistance / travelDistance;

        // Clamp progress to prevent overshooting
        travelProgress = Mathf.Clamp01(travelProgress);

        // Use Vector3.Lerp for smooth, constrained movement
        Vector3 startPosition = isAtTarget ? targetPoint.position : transform.position;
        Vector3 endPosition = targetPoint.position;

        transform.position = Vector3.Lerp(startPosition, endPosition, travelProgress);

        // Snap to final position when close enough
        if (travelProgress >= 0.999f || Vector3.Distance(transform.position, targetPoint.position) <= positionSnapThreshold)
        {
            transform.position = targetPoint.position;
            isMoving = false;
            isAtTarget = true;
            travelProgress = 1f;
            OpenDoor();
        }
    }

    // FIXED: Better player synchronization using parenting
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.transform;
            playerOnElevator = true;

            // Parent the player to the elevator for smooth movement
            player.SetParent(transform);

            StartElevator();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerOnElevator = false;

            // Unparent the player
            if (player != null)
            {
                player.SetParent(null);
                player = null;
            }

            // Only close if elevator is not moving
            if (!isMoving)
                CloseDoor();
        }
    }
}