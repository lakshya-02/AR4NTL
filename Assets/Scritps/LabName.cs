using System.Collections;
using UnityEngine;
using UnityEngine.UI;   // Remove if you use TMP instead

public class LabName : MonoBehaviour
{
    [Header("Clickable Targets")]
    [SerializeField] GameObject pauchObject;
    [SerializeField] GameObject teslaObject;

    [Header("Visual Target (Mesh to recolor)")]
    [SerializeField] MeshRenderer targetRenderer;   // Material color will change
    [SerializeField] MeshFilter targetMeshFilter;   // Optional: vertex colors

    [Header("Label")]
    [SerializeField] Text label; // Assign a UI Text (or replace with TMP and adjust type)

    [Header("Transition Settings")]
    [SerializeField] float cycleSpeed = 1.0f;       // Speed along gradient
    [SerializeField] float vertexUpdateInterval = 0.05f;
    [SerializeField] Gradient pauchGradient;        // Warm colors
    [SerializeField] Gradient teslaGradient;        // Cool colors

    enum Mode { None, Pauch, Tesla }
    Mode currentMode = Mode.None;

    Coroutine colorRoutine;
    float t;

    void Start()
    {
        // Auto-create default gradients if not set
        if (pauchGradient == null || pauchGradient.colorKeys.Length == 0)
            pauchGradient = MakeGradient(
                Color.red, new Color(1f, 0.5f, 0f), Color.yellow, Color.red);
        if (teslaGradient == null || teslaGradient.colorKeys.Length == 0)
            teslaGradient = MakeGradient(
                new Color(0f, 0.2f, 1f), Color.cyan, new Color(0.4f, 0f, 1f), new Color(0f, 0.2f, 1f));
    }

    void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
            TrySelect(Input.mousePosition);
#endif
        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
                TrySelect(touch.position);
        }
    }

    void TrySelect(Vector2 screenPos)
    {
        var cam = Camera.main;
        if (!cam) return;
        var ray = cam.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out var hit)) return;

        if (pauchObject && hit.transform.gameObject == pauchObject)
            ActivatePauch();
        else if (teslaObject && hit.transform.gameObject == teslaObject)
            ActivateTesla();
    }

    void ActivatePauch()
    {
        if (currentMode == Mode.Pauch) return;
        currentMode = Mode.Pauch;
        SetLabel("Pauch Lab");
        RestartColorRoutine();
    }

    void ActivateTesla()
    {
        if (currentMode == Mode.Tesla) return;
        currentMode = Mode.Tesla;
        SetLabel("Tesla Lab");
        RestartColorRoutine();
    }

    void SetLabel(string txt)
    {
        if (label) label.text = txt;
    }

    void RestartColorRoutine()
    {
        if (colorRoutine != null) StopCoroutine(colorRoutine);
        t = 0f;
        colorRoutine = StartCoroutine(ColorCycle());
    }

    IEnumerator ColorCycle()
    {
        Gradient g = (currentMode == Mode.Pauch) ? pauchGradient : teslaGradient;
        if (g == null) yield break;

        var wait = new WaitForSeconds(vertexUpdateInterval);
        Mesh mesh = targetMeshFilter ? targetMeshFilter.mesh : null;
        Color[] verts = null;
        if (mesh && mesh.vertexCount > 0)
            verts = new Color[mesh.vertexCount];

        while (currentMode != Mode.None)
        {
            t += Time.deltaTime * cycleSpeed;
            float u = Mathf.Repeat(t, 1f);
            Color c = g.Evaluate(u);
            ApplyColor(c, mesh, verts);
            yield return wait;
        }
    }

    void ApplyColor(Color c, Mesh mesh, Color[] verts)
    {
        if (targetRenderer)
        {
            // Use material instance
            var mat = targetRenderer.material;
            mat.color = c;
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", c * 0.5f);
        }
        if (mesh != null && verts != null)
        {
            for (int i = 0; i < verts.Length; i++)
                verts[i] = c;
            mesh.colors = verts;
        }
    }

    Gradient MakeGradient(params Color[] colors)
    {
        var g = new Gradient();
        int n = colors.Length;
        var cks = new GradientColorKey[n];
        var aks = new GradientAlphaKey[n];
        for (int i = 0; i < n; i++)
        {
            float t = (n == 1) ? 0f : (float)i / (n - 1);
            cks[i] = new GradientColorKey(colors[i], t);
            aks[i] = new GradientAlphaKey(colors[i].a, t);
        }
        g.SetKeys(cks, aks);
        return g;
    }

    // Optional external triggers
    public void TriggerPauch() => ActivatePauch();
    public void TriggerTesla() => ActivateTesla();
}
