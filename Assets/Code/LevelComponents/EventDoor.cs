using System;
using System.Collections;
using System.Linq;
using Code.Misc;
using UnityEngine;
using UnityEngine.Serialization;

public class EventDoor : MonoBehaviour
{

    private Animator _animator;
    public string requiredPersistentEvent = "";
    public Renderer WireRenderer;
    
    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        WireRenderer.material.SetFloat("_OnWeight", 0f);
        UpdateDoorState();
    }

    private void UpdateDoorState()
    {
        
        if (SaveSystem.GetPersistentEventCompleted(requiredPersistentEvent))
        {
            Util.ReplaceAnimatorTrigger(_animator, "Open");
            WireRenderer.material.SetFloat("_OnWeight", 1f);
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
    
    private void OnSaveDataUpdated(SaveSystem.SaveData obj)
    {
        UpdateDoorState();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    
    
}
