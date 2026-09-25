using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using DG.Tweening;
using FMOD.Studio;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Wasp;
using Util = Code.Misc.Util;

public partial class ProfessorFsm : CutsceneFsm
{
    public class ProfessorFsmState : CutsceneFsmState
    {
        public static int Busy;
        public static int Shocked;
        public static int SpeakingMural;
        public static int ShockedToSpeakingMural;

        public static int Speaking;
        public static int MuralIdle;
    }

    public class ProfessorFsmTrigger : CutsceneFsm.CutsceneFsmTrigger
    {
        public static int OnInteracted;
        public static int OnDialogueCompleted;
        public static int OnNodeCompleted;
    }

    protected override void OnAwake()
    {
        base.OnAwake();
        _interactable = GetComponentInChildren<Interactable>();
        _dialogueController = GetComponentInChildren<DialogueController>();
        Animator = GetComponentInChildren<Animator>();
        _halo = transform.Find("Halo").gameObject;
        _halo.SetActive(false);

    }
    

    protected override void OnStart()
    {
        base.OnStart();
        InitState = ProfessorFsmState.Busy;
        baseHeadlightIntensity = HeadLight.intensity;

    }
    
    public override void OnUpdate()
    {
        base.OnUpdate();

        var lightStrength = Mathf.Lerp(HeadLight.intensity, Machine.IsInState(ProfessorFsmState.Speaking) ? 0f : baseHeadlightIntensity, Time.deltaTime * 10f);
        HeadLight.intensity = lightStrength;

    }

    protected override void OnStartComplete()
    {
        base.OnStartComplete();
        Machine.Jump(ProfessorFsmState.Busy);
        OnSaveDataUpdated(SaveSystem.LoadCachedSaveData());
    }

    protected override void OnStateChanged(TriggerParams triggerParams)
    {
        base.OnStateChanged(triggerParams);
    }

    private void OnEnable()
    {
        _interactable.OnInteracted += OnInteracted;
        _dialogueController.OnCompleted += OnDialogueCompleted;
        _dialogueController.OnProgressed += OnDialogueProgressed;
        SaveSystem.OnSaveDataUpdated += OnSaveDataUpdated;

        _ambienceA =
            FMODUnity.RuntimeManager.CreateInstance(
                FMODUnity.RuntimeManager.PathToEventReference("event:/TerminalAmbientA"));
        
        _ambienceB =
            FMODUnity.RuntimeManager.CreateInstance(
                FMODUnity.RuntimeManager.PathToEventReference("event:/GlyphAmbience"));

        FMODUnity.RuntimeManager.AttachInstanceToGameObject(_ambienceA, gameObject);
        FMODUnity.RuntimeManager.AttachInstanceToGameObject(_ambienceB, gameObject);
        _ambienceA.start();
        // _ambienceB.start();
    }

    private void OnDisable()
    {
        _interactable.OnInteracted -= OnInteracted;
        _dialogueController.OnCompleted -= OnDialogueCompleted;
        _dialogueController.OnProgressed -= OnDialogueProgressed;
        SaveSystem.OnSaveDataUpdated -= OnSaveDataUpdated;
        
        _ambienceA.stop(STOP_MODE.ALLOWFADEOUT);
        _ambienceB.stop(STOP_MODE.ALLOWFADEOUT);
    }
}
