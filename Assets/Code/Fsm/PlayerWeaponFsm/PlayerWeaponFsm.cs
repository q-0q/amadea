using System;
using Cinemachine;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.Serialization;
using Wasp;

public partial class PlayerWeaponFsm : Fsm
{
    public class PlayerWeaponFsmState : FsmState
    {
        public static int Disabled;
        public static int Idle;
        public static int ImpaleStartup;
        public static int ImpaleActive;
        public static int ImpaleRecovery;
        public static int ImpaleStuck;
        public static int ImpalePlayerMounted;
        public static int ImpaleStuckRecovery;
    }

    public class PlayerWeaponFsmTrigger : FsmTrigger
    {
        public static int PlayerImpaleStateEntered;
        public static int PlayerGrappleStateEntered;
        public static int HitTerrain;
    }

    protected override void OnStart()
    {
        base.OnStart();
        InitState = PlayerWeaponFsmState.Disabled;
        transform.SetParent(null);
        _subTransform = transform.GetChild(0);
        _subTransformBaseLocalPosition = _subTransform.localPosition;
        TryGetComponent(out _impulseSource);
    }

    protected override void OnAwake()
    {
        base.OnAwake();

        // Singleton = this;
    }


    public override void OnUpdate()
    {
        base.OnUpdate();
        OnPlayerWeaponPositionUpdated?.Invoke(transform.position, Machine.IsInState(PlayerWeaponFsmState.ImpaleStartup) || Machine.IsInState(PlayerWeaponFsmState.ImpaleActive) || Machine.IsInState(PlayerWeaponFsmState.ImpaleStuck));
        
        
        if (Machine.IsInState(PlayerWeaponFsmState.Idle))
        {
            IdleOnUpdate();
        }
        
        if (Machine.IsInState(PlayerWeaponFsmState.ImpaleStartup))
        {
            ImpaleStartupOnUpdate();
        }
        else
        {
            _subTransform.localPosition = Vector3.Lerp(_subTransform.localPosition, _subTransformBaseLocalPosition, Time.deltaTime * ImpaleStartupPullbackSpeed * 5f);
        }
        
        if (Machine.IsInState(PlayerWeaponFsmState.ImpaleActive))
        {
            ImpaleActiveOnUpdate();
        }

        if (Machine.IsInState(PlayerWeaponFsmState.ImpaleStuckRecovery))
        {
            ImpaleStuckRecoveryOnUpdate();
        }
        
        if (Machine.IsInState(PlayerWeaponFsmState.ImpaleStuck))
        {
            ImpaleStuckOnUpdate();
        }
        
        if (Machine.IsInState(PlayerWeaponFsmState.ImpalePlayerMounted))
        {
            ImpalePlayerMountedOnUpdate();
        }
        
    }

    private void OnEnable()
    {
        PlayerFsm.OnPlayerImpaleStateEntered += OnPlayerImpaleEnter;
        PlayerFsm.OnPlayerGrappleStateEntered += OnPlayerGrappleEnter;
        PlayerWeaponCollider.OnPlayerWeaponCollision += OnWeaponCollision;
    }

    private void OnDisable()
    {
        PlayerFsm.OnPlayerImpaleStateEntered -= OnPlayerImpaleEnter;
        PlayerFsm.OnPlayerGrappleStateEntered -= OnPlayerGrappleEnter;
        PlayerWeaponCollider.OnPlayerWeaponCollision -= OnWeaponCollision;
    }
}