using UnityEngine;

public partial class PlayerFsm
{

    private void ClimbOnUpdate()
    {
        // YVelocity = Mathf.Lerp(0, -10f, Mathf.InverseLerp(ClimbMinimumDuration + 0.1f, ClimbMinimumDuration + 0.3f, TimeInCurrentState()));
    }
    
    private void ClimbConfigure()
    {
        Machine.Configure(PlayerFsmState.Climb)
            .SubstateOf(GravityFsmState.Aerial)
            .SubstateOf(PlayerFsmState.ForceWallRotation)
            .SubstateOf(PlayerFsmState.DontApplyGustYVelocity)
            .SubstateOf(PlayerFsmState.DontApplyYVelocity)
            .SubstateOf(GravityFsmState.RespectParentTransform)
            .Permit(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.Landsquat)
            .PermitIf(PlayerFsmTrigger.FaceOpen, PlayerFsmState.Fall, _ => TimeInCurrentState() > WallsquatMinimumDuration)
            .PermitIf(PlayerFsmTrigger.Jump, PlayerFsmState.Wallstep,
                _ => TimeInCurrentState() > WallstepMinimumDuration, 1)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.Fall)
            .OnEntry(_ =>
            {
                YVelocity = 0;
                LastUpwardsPosition = transform.position;
                _wallsquattedSinceLeavingGround = true;
                FMODUnity.RuntimeManager.PlayOneShotAttached(landFmodEvent, gameObject);
            })
            .OnExit(_ =>
            {
                YVelocity = 0;
            })
            .OnExitFrom(PlayerFsmTrigger.FaceOpen, _ => { _momentum = 0; });
    }
}