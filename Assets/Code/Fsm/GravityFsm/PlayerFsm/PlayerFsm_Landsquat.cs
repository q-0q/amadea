using UnityEngine;

public partial class PlayerFsm
{
    private void LandsquatConfigure()
    {
        Machine.Configure(PlayerFsmState.Landsquat)
            .SubstateOf(GravityFsmState.Grounded)
            .SubstateOf(PlayerFsmState.LockMomentum)
            .SubstateOf(PlayerFsmState.TinsicaUsable)
            .Permit(PlayerFsmTrigger.Jump, PlayerFsmState.Jumpsquat)
            .PermitIf(PlayerFsmTrigger.Jump, PlayerFsmState.Skipsquat, _ => _timeSinceDashFinished <= SkipWindowDuration, 1)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.GroundMove)
            .PermitIf(FsmTrigger.Timeout, PlayerFsmState.StepEnd, _ => _momentum < IdleMomentumThreshold, 1)
            // .PermitIf(FsmTrigger.Timeout, PlayerFsmState.Slide,
                // _ => parentTransform.gameObject.layer == LayerMask.NameToLayer("ForceSlide"), 2)
            .OnEntry(_ =>
            {
                Animator.SetLayerWeight(1, 0);
                FMODUnity.RuntimeManager.PlayOneShotAttached(landFmodEvent, gameObject);
                
                OnPlayerFootstep();
            })
            .OnEntryFrom(GravityFsmTrigger.StartFrameGrounded, @params =>
            {
                if (@params is not RaycastHitParam raycastHitParam) return;
                Debug.DrawLine(transform.position + Vector3.up * 5f, raycastHitParam.Hit.point, Color.yellow, 1f);
            })
            .OnExit(_ =>
            {
                _wallsquattedSinceLeavingGround = false;
                _dashSinceLeavingGround = false;
                _previousWallrunSide = FlankType.None;
                _currentFlankType = FlankType.None;
                currentRopeSwing = null;
                
                _movementAnimationMirror = !_movementAnimationMirror;
                var flip = _movementAnimationMirror ? 0 : 1f;
                Animator.SetFloat("Flip", flip);
                
            });

        Machine.Configure(PlayerFsmState.LandsquatAfterDash)
            .SubstateOf(PlayerFsmState.Landsquat);
        
        Machine.Configure(PlayerFsmState.LandsquatAfterSurge)
            .SubstateOf(PlayerFsmState.Landsquat);
        
        // Machine.Configure(PlayerFsmState.LandsquatTightrope)
        //     .PermitIf(FsmTrigger.Timeout, PlayerFsmState.TightropeMove, _=> true, 1)
        //     .SubstateOf(PlayerFsmState.Landsquat);
        
    }
}