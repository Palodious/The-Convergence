using UnityEngine;
public class ScrollUV : MonoBehaviour
{
    public float speedX = 0f;
    public float speedY = -0.5f;
    Renderer r;
    void Start() { r = GetComponent<Renderer>(); }
    void Update()
    {
        Vector2 offset = r.material.mainTextureOffset;
        offset += new Vector2(speedX, speedY) * Time.deltaTime;
        r.material.mainTextureOffset = offset;
    }
}