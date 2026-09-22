using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Identifiers;
using Wasp;

public partial class SentryFsm : Fsm
{
    public class SentryFsmState : FsmState
    {
        public static int Idle;
        public static int Wake;
        public static int Tracking;
        // public static int Extrapolating;
        // public static int Searching;
        public static int Firing;
    }

    public class SentryFsmTrigger : FsmTrigger
    {
        public static int PlayerInView;
        public static int PlayerOutOfView;
        public static int Shoot;
    }

    protected override void OnAwake()
    {
        base.OnAwake();
        _lineRenderer = GetComponentInChildren<LineRenderer>();
        _lineRenderer.SetPosition(0, eye.position);
        _lineRenderer.SetPosition(1, eye.position);
        _laserEnd = transform.Find("LaserEnd").gameObject;
        _laserEnd.SetActive(false);
    }

    protected override void OnStart()
    {
        base.OnStart();
        InitState = SentryFsmState.Idle;

    }

    protected override void OnStartComplete()
    {
        Machine.OnTransitionCompleted(OnStateChangeCompleted);
    }
    
    public override void OnUpdate()
    {
        base.OnUpdate();

        _obstructionTimer += Time.deltaTime;
        
        _lineRenderer.SetPosition(0, eye.position);
        
        if (Machine.IsInState(SentryFsmState.Idle))
        {
            _lineRenderer.SetPosition(1, eye.position);
            eye.rotation = Quaternion.Lerp(eye.rotation,Quaternion.LookRotation(transform.forward, Vector3.up), Time.deltaTime * 1);
        }
        
        if (Machine.IsInState(SentryFsmState.Wake))
        {
            
            _lineRenderer.SetPosition(1, eye.position);
            eye.rotation = Quaternion.Lerp(eye.rotation,Quaternion.LookRotation(GetPlayerPosition() - eye.position, Vector3.up), Time.deltaTime * 15);
            
        }
        
        if (Machine.IsInState(SentryFsmState.Tracking))
        {

            UpdateTrackingVelocity();
            RotateWithTrackingVelocity();
            UpdateLineRenderer();

            if (TimeInCurrentState() > 1f && !_blinking && !passive)
            {
                _blinking = true;
                _blinkTimer = 0f;
            };
            
            if (PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.SentryImmune) && (!_blinking || (_blinking && _blinkTimer < 0.5f))) Machine.Jump(SentryFsmState.Idle);


        }
        
        // if (Machine.IsInState(SentryFsmState.Extrapolating))
        // {
        //     DampenTrackingVelocity();
        //     RotateWithTrackingVelocity();
        //     UpdateLineRenderer();
        // }
        //
        // if (Machine.IsInState(SentryFsmState.Searching))
        // {
        //     var f = 1f;
        //     if (TimeInCurrentState() < f)
        //     {
        //         currentAngularVelocity = Vector3.Lerp(_searchEnterSpeed, -_searchEnterSpeed * 1.5f, Mathf.InverseLerp(0, f, TimeInCurrentState()));
        //     }
        //     else
        //     {
        //         currentAngularVelocity = Vector3.Lerp(-_searchEnterSpeed * 1.5f, Vector3.zero, Mathf.InverseLerp(f, 2f, TimeInCurrentState()));
        //     }
        //     
        //     RotateWithTrackingVelocity();
        //     UpdateLineRenderer();
        // }
        
        if (Machine.IsInState(SentryFsmState.Firing))
        {
            UpdateTrackingVelocity();
            RotateWithTrackingVelocity();
            
            _lineRenderer.SetPosition(1, eye.position);
        }

        if (_blinking) _blinkTimer += Time.deltaTime;
        
        _lineRenderer.material.SetFloat("_BlinkWeight", _blinking ? 1f: 0f);

    }

    private void OnStateChangeCompleted(TriggerParams? triggerParams)
    {
        print(InheritableEnum.GetFieldNameByValue(Machine.State(), typeof(SentryFsmState)));
    }




    private void OnEnable()
    {
    }

    private void OnDisable()
    {
    }
    
}
