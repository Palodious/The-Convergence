using UnityEngine;

public class bossDoor : MonoBehaviour
{
    [SerializeField] GameObject doorToDestroy;

    void Update()
    {
        if (gamemanager.instance.GetGameGoalCount() <= 1 && doorToDestroy != null)
        {
            Destroy(doorToDestroy);
        }
    }
}