using UnityEngine;

public partial class PlayerFsm
{

    private void WallsquatOnUpdate()
    {
        YVelocity = Mathf.Lerp(0, -10f, Mathf.InverseLerp(WallsquatMinimumDuration + 0.2f, WallsquatMinimumDuration + 0.4f, TimeInCurrentState()));
    }
    
    private void WallsquatConfigure()
    {
        Machine.Configure(PlayerFsmState.Wallsquat)
            .SubstateOf(GravityFsmState.Aerial)
            .SubstateOf(PlayerFsmState.ForceWallRotation)
            .SubstateOf(PlayerFsmState.DontApplyGustYVelocity)
            .SubstateOf(GravityFsmState.IgnoreVerticalDepenetration)
            // .SubstateOf(GravityFsmState.DontApplyYVelocity)
            .SubstateOf(GravityFsmState.RespectParentTransform)
            .SubstateOf(PlayerFsmState.MinorLeylineInteractable)
            .PermitIf(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.Landsquat, @params => !IsSlideTrigger(@params))
            .PermitIf(PlayerFsmTrigger.FaceOpen, PlayerFsmState.Fall, _ => TimeInCurrentState() > WallsquatMinimumDuration)
            .PermitIf(PlayerFsmTrigger.Jump, PlayerFsmState.Wallstep,
                _ => TimeInCurrentState() > WallstepMinimumDuration, 1)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.Fall)

            .OnEntry(_ =>
            {
                isSprinting = false;
                EndSurge();
                YVelocity = 0;
                LastUpwardsY = transform.position.y;
                _wallsquattedSinceLeavingGround = true;
                OnPlayerFootstep();
                FMODUnity.RuntimeManager.PlayOneShotAttached(landFmodEvent, gameObject);
                _momentum = 12f;
            })
            .OnExit(_ =>
            {
                YVelocity = 0;
            })
            .OnExitFrom(PlayerFsmTrigger.FaceOpen, _ => { _momentum = 0; })
            .OnExitFrom(FsmTrigger.Timeout, _ => { _momentum = 0; });
    }
}