using System;
using Code.Misc;
using UnityEngine;

public class LeylineDoor : MonoBehaviour
{
    public string metaName;
    public string requiredNode;
    private Animator _animator;
    private Collider _collider;
    public Renderer glowRenderer;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        TryGetComponent(out _collider);
        _collider.enabled = false;
        

        if (SaveSystem.GetTerminalNode(requiredNode))
        {
            glowRenderer.material.SetFloat("_GlowWeight", 1f);
            _collider.enabled = !SaveSystem.GetPersistentEventCompleted(metaName);
        }
        
        if (SaveSystem.GetPersistentEventCompleted(metaName))
        {
            Util.ReplaceAnimatorTrigger(_animator, "Open");
        }
        else
        {
            Util.ReplaceAnimatorTrigger(_animator, "Closed");
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
        if (SaveSystem.GetTerminalNode(requiredNode))
        {
            glowRenderer.material.SetFloat("_GlowWeight", 1f);
            if (!SaveSystem.GetPersistentEventCompleted(metaName)) _collider.enabled = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        DoOpen();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void DoOpen()
    {
        SaveSystem.WritePersistentEvent(metaName);
        _collider.enabled = false;
        Util.ReplaceAnimatorTrigger(_animator, "Opening");
        FMODUnity.RuntimeManager.PlayOneShotAttached(FMODUnity.RuntimeManager.PathToEventReference("event:/LeylineDoorOpen"), gameObject);
    }
}
