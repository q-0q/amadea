using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using Code.TriggerParams;
using FMOD.Studio;
using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public partial class ProfessorFsm
{
    private Interactable _interactable;
    private DialogueController _dialogueController;
    private const string PersistentEvent = "c1-snail-quest";
    [SerializeField] private Light HeadLight;
    private float baseHeadlightIntensity;
    private GameObject _halo;


    private EventInstance _ambienceA;
    private EventInstance _ambienceB;
    
    private void OnDialogueProgressed(int textIndex)
    {

    }

    private void OnSaveDataUpdated(SaveSystem.SaveData saveData)
    {
        // if (!saveData.terminalNodes.Contains("summit-glyph")) return;
        Machine.Fire(ProfessorFsmTrigger.OnNodeCompleted);
    }
}