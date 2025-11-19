using UnityEngine;

public class PlatformAttach : MonoBehaviour
{
    [SerializeField] string playerTag = "Player";

    private Vector3 lastPosition;
    private Vector3 platformVelocity;
    private bool playerOnPlatform;
    private Transform player;

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void Update()
    {
        platformVelocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;

        if (playerOnPlatform && player != null)
        {
            // Move player by same delta as platform
            player.position += platformVelocity * Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            player = other.transform;
            playerOnPlatform = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            player = null;
            playerOnPlatform = false;
        }
    }


}
