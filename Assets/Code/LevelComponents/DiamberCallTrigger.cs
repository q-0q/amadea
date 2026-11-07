using System;
using System.Linq;
using FMOD.Studio;
using UnityEngine;

public class DiamberCallTrigger : MonoBehaviour
{
    private TriggerProxy _triggerProxy;
    public string persistentEvent = "call-";
    private DialogueController _dialogueController;

    private EventInstance _eventInstance;

    private void Awake()
    {
        _triggerProxy = GetComponentInChildren<TriggerProxy>();
        _dialogueController = GetComponentInChildren<DialogueController>();
        _eventInstance = FMODUnity.RuntimeManager.CreateInstance("event:/CallRinging");
    }

    private void OnEnable()
    {
        _triggerProxy.OnTriggerProxyStay += OnTrigger;
        _dialogueController.OnProgressed += OnProgressed;
    }

    private void OnProgressed(int obj)
    {
        _eventInstance.stop(STOP_MODE.ALLOWFADEOUT);
        if (obj == 1) FMODUnity.RuntimeManager.PlayOneShotAttached("event:/CallAnswered", PlayerFsm.Singleton.gameObject);
    }

    private void OnDisable()
    {
        _triggerProxy.OnTriggerProxyStay -= OnTrigger;
        _dialogueController.OnProgressed -= OnProgressed;
        
        _eventInstance.stop(STOP_MODE.ALLOWFADEOUT);
    }

    private void OnTrigger(Collider other)
    {
        if (!SaveSystem.GetAllItems().Contains("Map")) return;
        if (SaveSystem.GetPersistentEventCompleted(persistentEvent)) return;
        if(!PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.Interactable)) return;
        SaveSystem.WritePersistentEvent(persistentEvent);
        DialogueCanvas.Singleton.StartDialogue(_dialogueController);
        PlayerFsm.Singleton.Machine.Jump(PlayerFsm.PlayerFsmState.Dialogue);
        FMODUnity.RuntimeManager.AttachInstanceToGameObject(_eventInstance, PlayerFsm.Singleton.gameObject);
        _eventInstance.start();
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
