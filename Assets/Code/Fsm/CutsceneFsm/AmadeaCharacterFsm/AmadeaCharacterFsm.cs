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

public partial class AmadeaCharacterFsm : CutsceneFsm
{
    public class AmadeaCharacterFsmState : CutsceneFsmState
    {
        public static int Sit;
        public static int Stand;
        public static int SitToStand;
    }

    public class AmadeaCharacterFsmTrigger : CutsceneFsm.CutsceneFsmTrigger
    {
        
        public static int OnInteracted;
        public static int OnPlayerNear;
        public static int OnPlayerFar;
        public static int OnDialogueCompleted;
    }

    protected override void OnAwake()
    {
        base.OnAwake();
        _interactable = GetComponentInChildren<Interactable>();
        _dialogueController = GetComponentInChildren<DialogueController>();
        Animator = GetComponentInChildren<Animator>();

    }

    protected override void OnStart()
    {
        base.OnStart();
        InitState = AmadeaCharacterFsmState.Sit;
        
    }
    
    public override void OnUpdate()
    {
        base.OnUpdate();

        var toPlayer = PlayerFsm.Singleton.transform.position - transform.position;
        toPlayer = new Vector3(toPlayer.x, 0f, toPlayer.z);
        var angle = Vector3.SignedAngle(-transform.forward, toPlayer, Vector3.up);
        var turn = Mathf.InverseLerp(-70f, 90f, angle);
        Animator.SetFloat("HeadTurn", turn);
    }

    protected override void OnStartComplete()
    {
        base.OnStartComplete();
        
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
    }

    private void OnDisable()
    {
        _interactable.OnInteracted -= OnInteracted;
        _dialogueController.OnCompleted -= OnDialogueCompleted;
        _dialogueController.OnProgressed -= OnDialogueProgressed;
    }
}
