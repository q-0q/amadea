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

public partial class SavapheSimpleFsm : CutsceneFsm
{
    public class SavapheSimpleFsmState : CutsceneFsmState
    {
        public static int NotRung;
        public static int Rung;
    }

    public class SavapheSimpleFsmTrigger : CutsceneFsm.CutsceneFsmTrigger
    {
        public static int BellRung;
    }

    protected override void OnAwake()
    {
        base.OnAwake();
        _interactable = GetComponentInChildren<Interactable>();
        Animator = GetComponentInChildren<Animator>();

    }

    protected override void OnStart()
    {
        base.OnStart();
        InitState = SavapheSimpleFsmState.NotRung;
        
    }
    
    public override void OnUpdate()
    {
        base.OnUpdate();

        var playerNotCrossedDialogueDistance =
            Vector3.Distance(PlayerFsm.Singleton.transform.position, _interactable.transform.position);
        
        Animator.SetFloat("Look", Mathf.InverseLerp(25f, 15f, playerNotCrossedDialogueDistance));
        
    }

    protected override void OnStartComplete()
    {
        base.OnStartComplete();
        // Machine.Jump(SaveSystem.GetBell(bell.metaName)
        //     ? SavapheSimpleFsmState.Rung
        //     : SavapheSimpleFsmState.NotRung);
    }

    protected override void OnStateChanged(TriggerParams triggerParams)
    {
        base.OnStateChanged(triggerParams);
    }

    private void OnEnable()
    {
        // bell.GetComponentInChildren<Interactable>().OnInteracted += OnBellInteracted;
    }

    private void OnDisable()
    {
        // bell.GetComponentInChildren<Interactable>().OnInteracted -= OnBellInteracted;
    }
}
