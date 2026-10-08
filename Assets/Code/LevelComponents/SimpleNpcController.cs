using System;
using FMOD.Studio;
using UnityEngine;
using UnityEngine.Serialization;
using Util = Code.Misc.Util;

public class SimpleNpcController : MonoBehaviour
{
    private Animator _animator;

    public string animationTrigger = "";
    public string interactionEvent = "";
    public string altDialogueEvent = "";
    public string removalEvent = "";

    public DialogueController defaultDialogueController;
    public DialogueController altDialogueController;


    private void Awake()
    {
        if (removalEvent != ""){
            if (SaveSystem.GetPersistentEventCompleted(removalEvent))
            {
                Destroy(gameObject);
                return;
            }
        }

        _animator = GetComponentInChildren<Animator>();
        if (_animator != null) Util.ReplaceAnimatorTrigger(_animator, animationTrigger);
        
        var alt = false;
        if (altDialogueEvent != "") {
            if (SaveSystem.GetPersistentEventCompleted(altDialogueEvent))
            {
                alt = true;
            }
        }

        if (defaultDialogueController != null) defaultDialogueController.GetComponent<Interactable>().SetEnabled(!alt);
        if (altDialogueController != null) altDialogueController.GetComponent<Interactable>().SetEnabled(alt);
        

    }

    private void OnEnable()
    {
        if (defaultDialogueController != null)
            defaultDialogueController.GetComponent<Interactable>().OnInteracted += OnDialogueInteracted;
        
        if (altDialogueController != null)
            altDialogueController.GetComponent<Interactable>().OnInteracted += OnDialogueInteracted;
    }

    private void OnDisable()
    {
        if (defaultDialogueController != null)
            defaultDialogueController.GetComponent<Interactable>().OnInteracted -= OnDialogueInteracted;
        
        if (altDialogueController != null)
            altDialogueController.GetComponent<Interactable>().OnInteracted -= OnDialogueInteracted;
    }

    private void OnDialogueInteracted()
    {
        if (interactionEvent != "") SaveSystem.WritePersistentEvent(interactionEvent);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
