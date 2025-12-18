using UnityEngine;

public class PylonController : MonoBehaviour
{
    public bool isDestroyed = false;
    private MeshRenderer meshRenderer;
    private PylonOrbit orbit;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        orbit = GetComponent<PylonOrbit>();
    }

    public void OnHit()
    {
        if (isDestroyed) return;

        isDestroyed = true;

        // Visual feedback
        meshRenderer.material.color = Color.gray;
        meshRenderer.material.DisableKeyword("_EMISSION");

        // Stop orbiting
        orbit.enabled = false;

        // Tell the rift a pylon was destroyed
        Object.FindFirstObjectByType<RiftController>()?.PylonDestroyed();
    }
}