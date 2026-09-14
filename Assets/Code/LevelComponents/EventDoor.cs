using System;
using System.Collections;
using System.Linq;
using Code.Misc;
using UnityEngine;
using UnityEngine.Serialization;

public class EventDoor : MonoBehaviour
{

    private Animator _animator;
    private Interactable _interactable;
    public string requiredPersistentEvent = "";
    public string persistentEvent = "";
    private Light _light;
    public Renderer WireRenderer;
    
    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _interactable = GetComponentInChildren<Interactable>();
        _light = GetComponentInChildren<Light>();
        WireRenderer.material.SetFloat("_OnWeight", 0f);
        UpdateDoorState();
    }

    private void UpdateDoorState()
    {
        _interactable.SetEnabled(false);
        _light.enabled = false;
        
        if (SaveSystem.GetPersistentEventCompleted(persistentEvent)) Util.ReplaceAnimatorTrigger(_animator, "Open");
        else if (SaveSystem.GetPersistentEventCompleted(requiredPersistentEvent))
        {
            WireRenderer.material.SetFloat("_OnWeight", 1f);
            _interactable.SetEnabled(true);
            _light.enabled = true;
        }
    }

    private void OnEnable()
    {
        _interactable.OnInteracted += OnInteracted;
        SaveSystem.OnSaveDataUpdated += OnSaveDataUpdated;
        
    }


    private void OnDisable()
    {
        _interactable.OnInteracted -= OnInteracted;
        SaveSystem.OnSaveDataUpdated -= OnSaveDataUpdated;
    }

    private void OnInteracted()
    {
        Util.ReplaceAnimatorTrigger(_animator, "Opening");
        SaveSystem.WritePersistentEvent(persistentEvent);
        _interactable.SetEnabled(false);
        StartCoroutine(LightCoroutine());

        IEnumerator LightCoroutine()
        {
            var t = 0f;
            var d = 0.5f;
            var i = _light.intensity;
            while (t < d)
            {
                _light.intensity = Mathf.Lerp(i, 0, t / d);
                t += Time.deltaTime;
                yield return null;
            }

            _light.enabled = false;
        }
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
