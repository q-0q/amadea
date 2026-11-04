using Cinemachine;
using UnityEngine;

public partial class PlayerFsm
{
    private void TightropeMoveOnUpdate()
    {
        var tightropeStart = springCollider.tightropeController.transform;
        var tightropeEnd = springCollider.tightropeController.end;
        var v3 = GetInputMovementVector3();
        
        if (v3.magnitude < InputMagnitudeThreshhold) v3 = transform.forward;
        var angle = Vector3.Angle(v3, tightropeStart.position - transform.position);
        var target = angle < 90f ? tightropeStart : tightropeEnd;
        var toTarget = target.position - transform.position;
        var weight = Mathf.InverseLerp(20f, 70f, Mathf.Abs(angle - 90f));

        toTarget = Vector3.Lerp(v3, toTarget, weight);
        // toTarget = new Vector3(0f, toTarget.y, 0f);
        // var targetRotation = Quaternion.LookRotation(target.position - transform.position, GetUpVector());
        // // transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        
        
        HandleTurningCore(1.75f, 1f, toTarget);
        
        var weightedMomentum = Mathf.Lerp(0f, MaxMomentum, weight);
        var clampedMomentum = Mathf.Min(_momentum, weightedMomentum);
        _momentum = Mathf.Lerp(_momentum, clampedMomentum, Time.deltaTime * 20f);
        
        HandleInputMomentumChange();
        SetAnimatorMomentum();

        var collisionPlayerMove = ComputeCollisionMove(((target.position - GetUpVector() * GravityFsmSpringCollider.Sag) - springCollider.transform.position).normalized * ComputeDesiredMove().magnitude);
        springCollider.transform.parent.position += collisionPlayerMove;
        var collisionAlignmentMove = ComputeCollisionMove((springCollider.tightropeController.ClosestPointOnLine(transform.position) -
                                                           transform.position) * (Time.deltaTime * 5f));
        springCollider.transform.parent.position += collisionAlignmentMove;
        

        SetAnimatorSpeedMod();
    }

    private void TightropeMoveConfigure()
    {
        Machine.Configure(PlayerFsmState.TightropeMove)
            .SubstateOf(GravityFsmState.Grounded)
            .SubstateOf(PlayerFsmState.Interactable)
            .Permit(GravityFsmTrigger.StartFrameAerial, PlayerFsmState.Fall)
            .PermitIf(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.GroundMove,
                @params => !IsTightropeTrigger(@params))
            .Permit(PlayerFsmTrigger.Jump, PlayerFsmState.Jumpsquat)
            .PermitIf(PlayerFsmTrigger.Jump, PlayerFsmState.Skipsquat, _ => _timeSinceDashFinished <= SkipWindowDuration, 1)
            // .Permit(PlayerFsmTrigger.HardTurn, PlayerFsmState.HardTurn)
            // .PermitIf(PlayerFsmTrigger.Dash, PlayerFsmState.Dashsquat, CanDash)
            // .PermitIf(PlayerFsmTrigger.Attack, PlayerFsmState.ImpaleGround, CanImpale)
            // .PermitIf(PlayerFsmTrigger.Attack, PlayerFsmState.GrappleStartup, CanGrapple, 1)
            // .PermitIf(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.Slide, _ => _slopeTimer > 0.2f)
            .OnEntry(_ =>
            {
                _wallsquattedSinceLeavingGround = false;
                _dashSinceLeavingGround = false;
            })
            .OnExitFrom(PlayerFsmTrigger.Jump, _ =>
            {
                if (GetInputMovementVector3().magnitude < InputMagnitudeThreshhold) return;
                _momentum = Mathf.Min(MaxMomentum, _momentum + 5f);
            });
    }
}