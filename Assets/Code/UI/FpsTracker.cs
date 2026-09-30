
using System;
using TMPro;
using UnityEngine;

public class FpsTracker
    : MonoBehaviour
{
    private float fps = 60f;
    private TextMeshProUGUI _tmp;

    private void Awake()
    {
        _tmp = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        float newFPS = 1.0f / Time.unscaledDeltaTime;
        fps = Mathf.Lerp(fps, newFPS, 0.005f);
        _tmp.text = "FPS: " + ((int)fps) + " (" + (int)(fps * 100f / 120f) + "%)";
    }

    private void OnEnable()
    {
        MetaSaveSystem.OnMetaSaveDataUpdated += OnMetaSaveDataUpdated;
    }

    private void OnDisable()
    {
        MetaSaveSystem.OnMetaSaveDataUpdated -= OnMetaSaveDataUpdated;
    }

    private void OnMetaSaveDataUpdated(MetaSaveSystem.MetaSaveData metaSaveData)
    {
        _tmp.enabled = metaSaveData.enableFpsDisplay;
    }
}
