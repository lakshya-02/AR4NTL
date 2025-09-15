using System.Collections;
using UnityEngine;

public class colorchanger : MonoBehaviour
{
    public enum CurvePreset
    {
        EaseInOut,
        Linear,
        EaseIn,
        EaseOut,
        EaseInFastOutSlow,
        Pulse,
        Bounce,
        Overshoot
    }

    [Header("Materials / Timing")]
    [SerializeField] Material[] materials;
    [SerializeField] float changeInterval = 1.5f;
    [SerializeField] float transitionDuration = 0.75f;

    [Header("Curve (leave empty to auto-generate)")]
    [SerializeField] CurvePreset preset = CurvePreset.EaseInOut;
    [SerializeField] AnimationCurve curve = null;

    [Header("Behavior")]
    [SerializeField] bool randomOrder = false;
    [SerializeField] bool loop = true;
    [SerializeField] bool fadeEmission = true;

    Renderer rend;
    int currentIndex = 0;
    bool transitioning = false;
    float waitTimer = 0f;
    Material runtimeMat;          // Instance so we don't overwrite original shared material

    void Awake()
    {
        EnsureCurveBuilt();
        rend = GetComponent<Renderer>();
        if (rend && rend.material != null)
        {
            runtimeMat = Instantiate(rend.material);
            rend.material = runtimeMat;
        }
        if (materials != null && materials.Length > 0 && runtimeMat)
        {
            runtimeMat.color = materials[0].color;
            CopyEmission(materials[0], runtimeMat);
        }
    }

    void OnValidate()
    {
        if (curve == null || curve.keys == null || curve.keys.Length == 0)
            EnsureCurveBuilt();
    }

    void EnsureCurveBuilt()
    {
        curve = BuildPresetCurve(preset);
    }

    [ContextMenu("Rebuild Curve From Preset")]
    void RebuildCurveFromPreset()
    {
        curve = BuildPresetCurve(preset);
    }

    AnimationCurve BuildPresetCurve(CurvePreset p)
    {
        switch (p)
        {
            case CurvePreset.Linear:
                return new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));
            case CurvePreset.EaseIn:
                return new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 2f),
                    new Keyframe(1f, 1f, 0f, 0f)
                );
            case CurvePreset.EaseOut:
                return new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 0f),
                    new Keyframe(1f, 1f, 2f, 0f)
                );
            case CurvePreset.EaseInFastOutSlow:
                return new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 3.5f),
                    new Keyframe(0.6f, 0.9f, 0.5f, 0.5f),
                    new Keyframe(1f, 1f, 0f, 0f)
                );
            case CurvePreset.Pulse:
                return new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 4f),
                    new Keyframe(0.2f, 1f, 0f, 0f),
                    new Keyframe(0.4f, 0.1f, 0f, 0f),
                    new Keyframe(0.6f, 1f, 0f, 0f),
                    new Keyframe(1f, 0f, 0f, 0f)
                );
            case CurvePreset.Bounce:
                return new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 6f),
                    new Keyframe(0.55f, 1.1f, 0f, 0f),
                    new Keyframe(0.75f, 0.92f, 0f, 0f),
                    new Keyframe(0.88f, 1.02f, 0f, 0f),
                    new Keyframe(1f, 1f, 0f, 0f)
                );
            case CurvePreset.Overshoot:
                return new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 3f),
                    new Keyframe(0.7f, 1.1f, 0f, 0f),
                    new Keyframe(1f, 1f, 0f, 0f)
                );
            case CurvePreset.EaseInOut:
            default:
                return AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }
    }

    void Update()
    {
        if (materials == null || materials.Length < 2 || (!loop && currentIndex == materials.Length - 1))
            return;
        if (transitioning) return;

        waitTimer += Time.deltaTime;
        if (waitTimer >= changeInterval)
        {
            waitTimer = 0f;
            int next = GetNextIndex();
            StartCoroutine(TransitionTo(next));
        }
    }

    int GetNextIndex()
    {
        if (randomOrder)
        {
            if (materials.Length <= 1) return currentIndex;
            int r;
            do { r = Random.Range(0, materials.Length); } while (r == currentIndex);
            return r;
        }
        int next = currentIndex + 1;
        if (next >= materials.Length)
            next = loop ? 0 : materials.Length - 1;
        return next;
    }

    IEnumerator TransitionTo(int nextIndex)
    {
        transitioning = true;

        var fromColor = runtimeMat.color;
        var toColor = materials[nextIndex].color;

        Color fromEmission = Color.black;
        Color toEmission = Color.black;
        bool hasEmission = fadeEmission && runtimeMat.HasProperty("_EmissionColor");

        if (hasEmission)
        {
            fromEmission = runtimeMat.GetColor("_EmissionColor");
            if (materials[nextIndex].HasProperty("_EmissionColor"))
                toEmission = materials[nextIndex].GetColor("_EmissionColor");
        }

        float t = 0f;
        while (t < transitionDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / transitionDuration);
            k = curve.Evaluate(k);
            runtimeMat.color = Color.Lerp(fromColor, toColor, k);
            if (hasEmission)
            {
                var e = Color.Lerp(fromEmission, toEmission, k);
                runtimeMat.SetColor("_EmissionColor", e);
                DynamicGI.SetEmissive(rend, e);
            }
            yield return null;
        }

        currentIndex = nextIndex;
        transitioning = false;
    }

    void CopyEmission(Material src, Material dst)
    {
        if (dst.HasProperty("_EmissionColor") && src.HasProperty("_EmissionColor"))
        {
            var c = src.GetColor("_EmissionColor");
            dst.SetColor("_EmissionColor", c);
            DynamicGI.SetEmissive(rend, c);
        }
    }

    // Runtime controls
    public void SetInterval(float seconds) => changeInterval = Mathf.Max(0f, seconds);
    public void SetTransitionDuration(float seconds) => transitionDuration = Mathf.Max(0.01f, seconds);
    public void SetRandom(bool random) => randomOrder = random;
    public void SetLoop(bool l) => loop = l;
    public void ForceNext()
    {
        if (!transitioning)
        {
            waitTimer = 0f;
            StartCoroutine(TransitionTo(GetNextIndex()));
        }
    }
}
