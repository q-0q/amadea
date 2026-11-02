
using UnityEngine;

public partial class PlayerFsm
{
    private void IdleOnUpdate()
    {
        if (GameMenu.Singleton.IsMenuOpen()) return;
        HandleInputMomentumChange();
        SetSafeGroundPosition();
        transform.position += ComputeCollisionMove(ApplyTractionNoTimescale(Vector3.zero) * Time.deltaTime);
    }
    
    private void StepStartOnUpdate()
    {
        if (GameMenu.Singleton.IsMenuOpen()) return;
        HandleInputMomentumChange();
        HandleTurning(2f);

        var movement = transform.forward * (1.5f);
        transform.position += ComputeCollisionMove(ApplyTractionNoTimescale(movement) * Time.deltaTime);
        SetAnimatorMomentum();
    }
    
    private void StepEndOnUpdate()
    {
        if (GameMenu.Singleton.IsMenuOpen()) return;
        HandleInputMomentumChange();
        transform.position += ApplyTractionNoTimescale(Vector3.zero) * Time.deltaTime;
    }
    
    private void StepConfigure()
    {

        
        Machine.Configure(PlayerFsmState.Idle)
            .SubstateOf(GravityFsmState.Grounded)
            .SubstateOf(PlayerFsmState.Interactable)
            .SubstateOf(PlayerFsmState.TinsicaUsable)
            .PermitIf(PlayerFsmTrigger.SwimTriggerRaycastHit, PlayerFsmState.SwimSurfaceRise, IsSwimTrigger)
            .Permit(PlayerFsm.PlayerFsmTrigger.Accelerating, PlayerFsm.PlayerFsmState.StepStart)
            .Permit(GravityFsmTrigger.StartFrameAerial, PlayerFsmState.Fall)
            .Permit(PlayerFsmTrigger.Jump, PlayerFsmState.Jumpsquat)
            .PermitIf(PlayerFsmTrigger.Jump, PlayerFsmState.Skipsquat,
                _ => _timeSinceDashFinished <= SkipWindowDuration, 1)
            .PermitIf(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.TightropeMove, IsTightropeTrigger, 6)
            .PermitIf(FsmTrigger.Timeout, PlayerFsmState.IdleLong, _ => !Machine.IsInState(PlayerFsmState.IdleLongLoop), 1);

        Machine.Configure(PlayerFsmState.IdleLong)
            .SubstateOf(PlayerFsmState.Idle);
        
        Machine.Configure(PlayerFsmState.IdleLongLoop)
            .SubstateOf(PlayerFsmState.Idle);
        
        Machine.Configure(PlayerFsmState.StepStart)
            .SubstateOf(GravityFsmState.Grounded)
            .SubstateOf(PlayerFsmState.Interactable)
            .SubstateOf(PlayerFsmState.TinsicaUsable)
            .PermitIf(PlayerFsmTrigger.SwimTriggerRaycastHit, PlayerFsmState.SwimSurfaceRise, IsSwimTrigger)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.GroundMove)
            .Permit(GravityFsmTrigger.StartFrameAerial, PlayerFsmState.Fall)
            .Permit(PlayerFsmTrigger.Jump, PlayerFsmState.Jumpsquat)
            .PermitIf(PlayerFsmTrigger.Jump, PlayerFsmState.Skipsquat,
                _ => _timeSinceDashFinished <= SkipWindowDuration, 1)
            .PermitIf(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.TightropeMove, IsTightropeTrigger, 6)
            .OnEntry(_ =>
            {
                OnPlayerFootstep();
            })
            .OnExit(_ =>
            {
                _momentum = Mathf.Max(_momentum, 3f);
            });
        
        Machine.Configure(PlayerFsmState.StepEnd)
            .SubstateOf(GravityFsmState.Grounded)
            .SubstateOf(PlayerFsmState.Interactable)
            .SubstateOf(PlayerFsmState.TinsicaUsable)
            .Permit(PlayerFsm.PlayerFsmTrigger.Accelerating, PlayerFsm.PlayerFsmState.StepStart)
            .PermitIf(PlayerFsmTrigger.SwimTriggerRaycastHit, PlayerFsmState.SwimSurfaceRise, IsSwimTrigger)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.Idle)
            .Permit(GravityFsmTrigger.StartFrameAerial, PlayerFsmState.Fall)
            .Permit(PlayerFsmTrigger.Jump, PlayerFsmState.Jumpsquat)
            .PermitIf(PlayerFsmTrigger.Jump, PlayerFsmState.Skipsquat,
                _ => _timeSinceDashFinished <= SkipWindowDuration, 1)
            .PermitIf(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.TightropeMove, IsTightropeTrigger, 6)
            .OnEntry(_ =>
            {
                OnPlayerFootstep();
                _previousPositionDeltaNoTimescale *= 0.35f;
                _momentum = 0;
                PlayerManaManager.Singleton.RestoreAll();
            });
        
    }
}