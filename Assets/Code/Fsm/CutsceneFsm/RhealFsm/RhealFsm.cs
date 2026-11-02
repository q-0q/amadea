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

public partial class RhealFsm : CutsceneFsm
{
    public class RhealFsmState : CutsceneFsmState
    {
        public static int Idle;
        public static int Speaking;
    }

    public class RhealFsmTrigger : CutsceneFsm.CutsceneFsmTrigger
    {

    }

    protected override void OnAwake()
    {
        base.OnAwake();
        _tinsicaDialogueController = transform.Find("TinsicaDialogue").GetComponent<DialogueController>();
        _altDialogueController = transform.Find("AltDialogue").GetComponent<DialogueController>();
        
        Animator = GetComponentInChildren<Animator>();

        _positionInstance = GetComponentInChildren<AdvancementController>().InstancePositionId;


        if (!SaveSystem.GetTrick("Tinsica") || SaveSystem.GetRhealTrickLearnedPositionInstance() == _positionInstance)
        {
            if (_altDialogueController != null) _altDialogueController.gameObject.SetActive(false);
            if (SaveSystem.GetTrick("Tinsica")) _tinsicaDialogueController.currentDialogueIndex = 3;
        }

        if (SaveSystem.GetTrick("Tinsica") && SaveSystem.GetRhealTrickLearnedPositionInstance() != _positionInstance)
        {
            _tinsicaDialogueController.gameObject.SetActive(false);
        }
        
        

    }

    protected override void OnStart()
    {
        base.OnStart();
        InitState = RhealFsmState.Idle;
        
    }
    
    public override void OnUpdate()
    {
        base.OnUpdate();
        
        var turnAmount = 0.5f;
        var pos = transform.InverseTransformPoint(PlayerFsm.Singleton.transform.position);
        turnAmount = Mathf.Lerp(0.2f, 0.8f, Mathf.InverseLerp(-8f, 8f, pos.x));
        Animator.SetFloat("Turn", Mathf.Lerp(Animator.GetFloat("Turn"), turnAmount, Time.deltaTime * 5f));

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
        _tinsicaDialogueController.OnCompleted += TinsicaDialogueCompleted;
        _tinsicaDialogueController.OnProgressed += TinsicaDialogueProgressed;
    }

    private void OnDisable()
    {
        _tinsicaDialogueController.OnCompleted -= TinsicaDialogueCompleted;
        _tinsicaDialogueController.OnProgressed -= TinsicaDialogueProgressed;
    }
}
