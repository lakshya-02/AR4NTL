using UnityEngine;

public class logoscript : MonoBehaviour
{
    [SerializeField] float degreesPerSecond = 10f;
    [SerializeField] string url = "https://geekprank.com/blue-death/";
    Camera cam;

    void Awake()
    {
        cam = Camera.main;
    }

    void Update()
    {
        transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.Self);
        HandlePointer();
    }

    void HandlePointer()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
            TryRay(Input.mousePosition);
#endif
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
                TryRay(t.position);
        }
    }

    void TryRay(Vector2 screenPos)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        var ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out var hit, 100f, ~0, QueryTriggerInteraction.Collide))
            if (hit.transform == transform)
                Application.OpenURL(url);
    }

    public void SetRotationSpeed(float dps) => degreesPerSecond = dps;
    public void SetUrl(string u) => url = u;
}
