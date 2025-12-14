using UnityEngine;

public class RiftController : MonoBehaviour
{
    private int pylonsDestroyed = 0;
    public int totalPylons = 3;

    public void PylonDestroyed()
    {
        pylonsDestroyed++;
        Debug.Log("Pylon destroyed! " + pylonsDestroyed + " / " + totalPylons);

        if (pylonsDestroyed >= totalPylons)
        {
            StartCoroutine(CollapseRift());
        }
    }

    System.Collections.IEnumerator CollapseRift()
    {
        // Quick collapse effect - scale down over 1 second
        float duration = 1f;
        Vector3 startScale = transform.localScale;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t / duration);
            yield return null;
        }

        Destroy(gameObject);
        Debug.Log("Rift sealed! Game Complete!");
    }
}