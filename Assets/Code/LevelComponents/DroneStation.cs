using System;
using UnityEngine;

public class DroneStation : MonoBehaviour
{
    private DroneFsm _drone;
    public Interactable interactable;
    private Transform _dronePosition;
    private DialogueController _dialogue;

    private void OnEnable()
    {
        interactable.OnInteracted += OnInteracted;
    }

    private void OnDisable()
    {
        interactable.OnInteracted -= OnInteracted;
    }

    private void OnInteracted()
    {
        _drone.Machine.Fire(DroneFsm.DroneFsmTrigger.StationInteract);
    }

    private void Awake()
    {
        _drone = GetComponentInChildren<DroneFsm>();
        _drone.SetDroneStation(this);
        _dronePosition = transform.Find("DronePosition");
        _drone.transform.position = _dronePosition.position;
        _drone.transform.rotation = _dronePosition.transform.rotation;

        _dialogue = GetComponentInChildren<DialogueController>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        interactable.SetEnabled((_drone.Machine.IsInState(DroneFsm.DroneFsmState.Idle) && !DroneFsm.IsAnyDroneActive) || _drone.Machine.IsInState(DroneFsm.DroneFsmState.Ready));
        interactable.text = _drone.Machine.IsInState(DroneFsm.DroneFsmState.Idle) ? "Deploy" : "Store";

        var dialoguePrefix = "Calibration drone status: ";
        _dialogue.dialogues[0].texts[0] = _drone.Machine.IsInState(DroneFsm.DroneFsmState.Idle) ? dialoguePrefix + "Ready." : dialoguePrefix + "Deployed.";
    }

    public Transform GetDronePosition()
    {
        return _dronePosition;
    }
}
