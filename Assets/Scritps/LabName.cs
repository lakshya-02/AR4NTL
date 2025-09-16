using System.Collections;
using UnityEngine;
using TMPro;

public class LabCube : MonoBehaviour
{
    [System.Serializable]
    public class LabInfo
    {
        public string labName;
        public Color labColor;
        public GameObject labUI;     // Parent for this lab (includes its children)
    }

    [Header("Lab Settings")]
    [SerializeField] LabInfo[] labs;
    [SerializeField] float cycleInterval = 1f;

    [Header("References")]
    [SerializeField] MeshRenderer cubeRenderer;
    [SerializeField] TextMeshPro labelText;

    [Header("Main UI (Always active with any lab)")]
    [SerializeField] GameObject mainTextUI;  // Parent with 2 background panels

    int currentIndex = 0;

    void Start()
    {
        if (labs.Length == 0) return;
        UpdateCubeVisuals();
        StartCoroutine(CycleLabs());
    }

    IEnumerator CycleLabs()
    {
        while (true)
        {
            yield return new WaitForSeconds(cycleInterval);
            currentIndex = (currentIndex + 1) % labs.Length;
            UpdateCubeVisuals();
        }
    }

    void UpdateCubeVisuals()
    {
        var lab = labs[currentIndex];
        if (cubeRenderer) cubeRenderer.material.color = lab.labColor;
        if (labelText) labelText.text = lab.labName;
    }

    void OnMouseDown()
    {
        if (labs.Length == 0) return;

        // First deactivate all lab UIs
        foreach (var lab in labs)
            if (lab.labUI) lab.labUI.SetActive(false);

        // Always activate the main text background
        if (mainTextUI) mainTextUI.SetActive(true);

        // Activate current lab with its children
        var activeLab = labs[currentIndex];
        if (activeLab.labUI) activeLab.labUI.SetActive(true);
    }
}
