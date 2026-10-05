using UnityEngine;

public partial class PlayerFsm
{
    private void GroundMoveOnUpdate()
    {
        _timeSinceJumpBufferable = 0f;
        if (_playerInput.actions["Sprint"].IsPressed()) isSprinting = true;
        
        HandleInputMomentumChange();
        HandleTurning(1f, false, 1f, false, isSprinting ? 0.5f : 1f);

        // var slopeMovementModifier = 1f;
        // if (GetGroundedRaycastHit(out var hit))
        // {
        //     var angle = Vector3.Angle(hit.normal, Vector3.up);
        //
        //     slopeMovementModifier = Mathf.Lerp(1f, 0.25f, Mathf.InverseLerp(30f, 70f, angle));
        // }
        
        HandleCollisionMove();

        SetAnimatorMomentum();
        SetAnimatorSpeedMod();

        if (!_swimSurfaceRippleQueued) StartCoroutine(SwimSurfaceRippleCoroutine());
        
        SetSafeGroundPosition();


        
        // if (WaterRaycast(out var swimRaycastParam))
        // {
        //     if (swimRaycastParam.distance > 3f && !_swimSurfaceRippleQueued) StartCoroutine(SwimSurfaceRippleCoroutine());
        // }
    }

    private void GroundMoveConfigure()
    {
        Machine.Configure(PlayerFsmState.GroundMove)
            .SubstateOf(GravityFsmState.Grounded)
            .SubstateOf(PlayerFsmState.Interactable)
            .PermitIf(PlayerFsmTrigger.FaceLedge, PlayerFsmState.Vault, CanVault, 1)
            .SubstateOf(PlayerFsmState.MinorLeylineInteractable)
            .SubstateOf(PlayerFsmState.TinsicaUsable)
            .Permit(PlayerFsmTrigger.Press, PlayerFsmState.Press)
            .PermitIf(PlayerFsmTrigger.SwimTriggerRaycastHit, PlayerFsmState.SwimSurfaceRise, IsSwimTrigger)
            .Permit(PlayerFsm.PlayerFsmTrigger.IdleMomentumThresholdPassedDecelerating, PlayerFsm.PlayerFsmState.StepEnd)
            .Permit(GravityFsmTrigger.StartFrameAerial, PlayerFsmState.Fall)
            .Permit(PlayerFsmTrigger.Jump, PlayerFsmState.Jumpsquat)
            .PermitIf(PlayerFsmTrigger.Jump, PlayerFsmState.Skipsquat, _ => _timeSinceDashFinished <= SkipWindowDuration, 1)
            .PermitIf(PlayerFsmTrigger.Jump, PlayerFsmState.TinsicaJump, _ => PlayerManaManager.Singleton.GetCurrentAvailableMana() >= 1 && _timeSinceTinsicaExited < CoyoteTime, 2)
            .PermitIf(PlayerFsmTrigger.HardTurn, PlayerFsmState.HardTurn, _=> _momentum > HardTurnMinimumMomentum)
            // .PermitIf(PlayerFsmTrigger.Dash, PlayerFsmState.Dashsquat, CanDash)
            .PermitIf(PlayerFsmTrigger.Attack, PlayerFsmState.ImpaleGround, CanImpale)
            .PermitIf(PlayerFsmTrigger.Attack, PlayerFsmState.GrappleStartup, CanGrapple, 1)
            // .PermitIf(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.Slide, _ => _slopeTimer > 0.2f)
            .PermitIf(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.TightropeMove, IsTightropeTrigger, 6)
            .SubstateOf(PlayerFsmState.SlideInteractable)
            .OnEntry(_ =>
            {
                _wallsquattedSinceLeavingGround = false;
                _dashSinceLeavingGround = false;
                _previousWallrunSide = FlankType.None;
                _currentFlankType = FlankType.None;
                _wallsquattedSinceLeavingGround = false;
                _dashSinceLeavingGround = false;
                currentRopeSwing = null;
            })
            .OnExitFrom(GravityFsmTrigger.StartFrameAerial, _ =>
            {
                PlaySlipSound();
            });
        
        
        Machine.Configure(PlayerFsmState.GroundMoveAfterVault)
            .SubstateOf(PlayerFsmState.GroundMove);
        
        Machine.Configure(GravityFsmState.Grounded)
            .SubstateOf(PlayerFsmState.DisplaceFoliage);
    }
}