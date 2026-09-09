using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Identifiers;
using UnityEngine.InputSystem;
using Wasp;

public partial class DroneFsm : Fsm
{
    public class DroneFsmState : FsmState
    {
        public static int Idle;
        public static int Deploying;
        public static int Ready;
        public static int Pulsing;
        public static int Storing;
    }

    public class DroneFsmTrigger : FsmTrigger
    {
        public static int StationInteract;
        public static int Pulse;
    }

    protected override void OnAwake()
    {
        base.OnAwake();
        IsAnyDroneActive = false;
        _lights = transform.Find("Lights").gameObject;
        // _lights.SetActive(false);
        _playerInput = GetComponent<PlayerInput>();
        _vibrator = transform.Find("Vibrator");
        _pulseParticles = GetComponentInChildren<ParticleSystem>();

    }

    protected override void OnStart()
    {
        base.OnStart();
        InitState = DroneFsmState.Idle;

    }

    protected override void OnStartComplete()
    {
        Machine.OnTransitionCompleted(OnStateChangeCompleted);
    }
    
    public override void OnUpdate()
    {
        base.OnUpdate();


        if (Machine.IsInState(DroneFsmState.Storing))
        {
            ReturnToIdlePosition(Mathf.Lerp(0f, 7f, Mathf.InverseLerp(0, 0.5f, TimeInCurrentState())));
        }
        
        // if (Machine.IsInState(DroneFsmState.Ready) || Machine.IsInState(DroneFsmState.Deploying) || Machine.IsInState(DroneFsmState.Pulsing))
        // {
        //     FollowPlayerRedux();
        // }

    }

    private void LateUpdate()
    {
        if (Machine.IsInState(DroneFsmState.Ready) || Machine.IsInState(DroneFsmState.Deploying) || Machine.IsInState(DroneFsmState.Pulsing))
        {
            FollowPlayer();
        }
    }

    private void OnStateChangeCompleted(TriggerParams? triggerParams)
    {
        
    }




    private void OnEnable()
    {
    }

    private void OnDisable()
    {
    }
    
}
