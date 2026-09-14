using System.Collections;
using DG.Tweening;
using UnityEngine;

public partial class PlayerFsm
{

    private void PressOnUpdate()
    {
        HandleTurning(0.5f);

        if (PressRaycast(out var hit))
        {
            // transform.position += ComputeCollisionMove(-hit.normal * (Time.deltaTime * 5f));
            var angle = Vector3.SignedAngle(-hit.normal, transform.forward, Vector3.up);
            var desired = Mathf.InverseLerp(-40f, 40f, angle);
            Animator.SetFloat("PressTurn", Mathf.Lerp(Animator.GetFloat("PressTurn"),desired, Time.deltaTime * 10f));
        }
    }
    
    private void PressConfigure()
    {
        Machine.Configure(PlayerFsmState.Press)
            .PermitIf(PlayerFsmTrigger.FaceOpen, PlayerFsmState.Idle, _ => !PressRaycast(out var _))
            .SubstateOf(GravityFsmState.Grounded)
            .SubstateOf(PlayerFsmState.Interactable)
            .SubstateOf(PlayerFsmState.TinsicaUsable)
            .PermitIf(PlayerFsmTrigger.SwimTriggerRaycastHit, PlayerFsmState.SwimSurfaceRise, IsSwimTrigger)
            .Permit(GravityFsmTrigger.StartFrameAerial, PlayerFsmState.Fall)
            .Permit(PlayerFsmTrigger.Jump, PlayerFsmState.Jumpsquat)
            .PermitIf(PlayerFsmTrigger.Jump, PlayerFsmState.Skipsquat,
                _ => _timeSinceDashFinished <= SkipWindowDuration, 1)
            .PermitIf(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.TightropeMove, IsTightropeTrigger, 6)
            .OnEntry(_ =>
            {
                OnPlayerFootstep();
                Animator.SetLayerWeight(1, 0f);
                isSprinting = false;
                EndSurge();
            })
            .OnExit(_ =>
            {
                Animator.SetLayerWeight(1, 0f);
            });

    }
}