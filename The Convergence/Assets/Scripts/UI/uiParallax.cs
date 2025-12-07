using UnityEngine;

// Small position offset based on mouse for layered parallax.
public class uiParallax : MonoBehaviour
{
    // Pixels of max offset.
    [SerializeField] private float range = 10f;
    [SerializeField] private float smooth = 8f;

    private RectTransform rt;
    private Vector3 startLocalPos;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        startLocalPos = rt.localPosition;
    }

    void OnEnable()
    {
        rt.localPosition = startLocalPos;
    }

    void Update()
    {
        Vector2 mouse = Input.mousePosition;

        float nx = ((mouse.x / Screen.width) - 0.5f) * 2f;
        float ny = ((mouse.y / Screen.height) - 0.5f) * 2f;

        Vector3 target = startLocalPos + new Vector3(nx * range, ny * range, 0f);
        rt.localPosition = Vector3.Lerp(rt.localPosition, target, smooth * Time.unscaledDeltaTime);
    }
}
