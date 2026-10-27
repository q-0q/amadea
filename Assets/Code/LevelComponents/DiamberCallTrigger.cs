using System;
using UnityEngine;

public class DiamberCallTrigger : MonoBehaviour
{
    private TriggerProxy _triggerProxy;
    public string persistentEvent = "call-";
    private DialogueController _dialogueController;
    

    private void Awake()
    {
        _triggerProxy = GetComponentInChildren<TriggerProxy>();
        _dialogueController = GetComponentInChildren<DialogueController>();
    }

    private void OnEnable()
    {
        _triggerProxy.OnTriggerProxyStay += OnTrigger;
    }

    private void OnDisable()
    {
        _triggerProxy.OnTriggerProxyStay -= OnTrigger;
    }

    private void OnTrigger(Collider other)
    {
        if (SaveSystem.GetPersistentEventCompleted(persistentEvent)) return;
        if(!PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.Interactable)) return;
        SaveSystem.WritePersistentEvent(persistentEvent);
        DialogueCanvas.Singleton.StartDialogue(_dialogueController);
        PlayerFsm.Singleton.Machine.Jump(PlayerFsm.PlayerFsmState.Dialogue);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _dialogueController.DialogueName = DialogueCanvas.Singleton.GetTextIndex() == 0 ? "" : "Silicant Diamber";
    }
}
