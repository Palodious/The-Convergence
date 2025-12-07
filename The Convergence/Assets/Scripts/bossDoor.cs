using UnityEngine;
using System.Collections;

public class bossDoor : MonoBehaviour
{
    [SerializeField] GameObject doorToDestroy;
    private bool doorOpened = false;

    void Update()
    {
        if (!doorOpened && gamemanager.instance.GetGameGoalCount() <= 1)
        {
            StartCoroutine(OpenDoor());
        }
    }

    IEnumerator OpenDoor()
    {
        doorOpened = true;

        // Show popup
        gamemanager.instance.bossDoorPopup.SetActive(true);

        // Destroy the door
        if (doorToDestroy != null)
            Destroy(doorToDestroy);

        // Wait a moment so player can see popup
        yield return new WaitForSeconds(2f);

        // Hide popup
        gamemanager.instance.bossDoorPopup.SetActive(false);
    }
}