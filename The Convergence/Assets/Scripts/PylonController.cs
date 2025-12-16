using UnityEngine;

public class PylonController : MonoBehaviour
{
    public bool isDestroyed = false;
    private MeshRenderer renderer;
    private PylonOrbit orbit;

    void Start()
    {
        renderer = GetComponent<MeshRenderer>();
        orbit = GetComponent<PylonOrbit>();
    }

    public void OnPulseHit()
    {
        if (isDestroyed) return;

        isDestroyed = true;

        // Visual feedback
        renderer.material.color = Color.gray;
        renderer.material.DisableKeyword("_EMISSION");

        // Stop orbiting
        orbit.enabled = false;

        // Tell the rift a pylon was destroyed
        Object.FindFirstObjectByType<RiftController>()?.PylonDestroyed();
    }
}