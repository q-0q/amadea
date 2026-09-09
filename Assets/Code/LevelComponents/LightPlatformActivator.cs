using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LightPlatformActivator : MonoBehaviour
{
    public string persistentEvent = "";

    private List<LightPlatform> _lightPlatforms;

    public Transform LightPlatformsParent = new RectTransform();
    
    public Renderer WireRenderer;

    private void Awake()
    {

        _lightPlatforms = LightPlatformsParent.GetComponentsInChildren<LightPlatform>().ToList();

    }

    private void Start()
    {
                
        if (SaveSystem.GetPersistentEventCompleted(persistentEvent))
        {
            TurnOn();
        }

        else
        {
            TurnOff();
        }

    }

    private void OnEnable()
    {
        SaveSystem.OnSaveDataUpdated += OnSaveDataUpdated;
    }

    private void OnDisable()
    {
        SaveSystem.OnSaveDataUpdated -= OnSaveDataUpdated;
    }

    private void OnSaveDataUpdated(SaveSystem.SaveData _)
    {
        if (SaveSystem.GetPersistentEventCompleted(persistentEvent))
        {
            TurnOn();
        }
    }

    void TurnOn()
    {
        foreach (var lightPlatform in _lightPlatforms)
        {
            lightPlatform.TurnOn();
        }
        
        WireRenderer.material.SetFloat("_OnWeight", 1f);
    }

    void TurnOff()
    {
        foreach (var lightPlatform in _lightPlatforms)
        {
            lightPlatform.TurnOff();
        }
        
        WireRenderer.material.SetFloat("_OnWeight", 0f);
    }
}
