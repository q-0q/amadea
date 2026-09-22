using System.Collections.Generic;
using Cinemachine;
using Code.TriggerParams;
using FMOD.Studio;
using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public partial class SavaphePitonUpperFsm
{
    // public BellController bell;
    private Interactable _interactable;

    private void OnBellInteracted()
    {
        Machine.Fire(SavaphePitonUpperFsmTrigger.BellRung);
    }



}