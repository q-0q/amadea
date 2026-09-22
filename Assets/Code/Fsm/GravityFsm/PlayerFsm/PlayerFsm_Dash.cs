using UnityEngine;

public partial class PlayerFsm
{
    private void DashOnUpdate()
    {
        HandleTurning(AirControlTurningMultiplier, true, AirControlTurningMomentumDecayModifier);
        Animator.SetLayerWeight(1, 0);
        var dashForwardSpeed = Mathf.Lerp(DashForwardSpeed, DashForwardSpeed - 4f, Mathf.InverseLerp(0.4f, 0.65f, TimeInCurrentState())) * GetCurrentSurgeSpeedMultiplier();
        
        var collisionMove = ComputeCollisionMove(transform.forward * (dashForwardSpeed * Time.deltaTime));
        transform.position += collisionMove;
    }

    private void DashConfigure()
    {
        Machine.Configure(PlayerFsmState.Dash)
            .SubstateOf(GravityFsmState.Aerial)
            .SubstateOf(PlayerFsmState.WallInteractable)
            .SubstateOf(PlayerFsmState.PitonInteractable)
            .SubstateOf(PlayerFsmState.RopeSwingInteractable)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.FallAfterDash)
            .PermitIf(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.Skipsquat, _ => _inputBuffer.IsBuffered("Jump"), 1)
            .PermitIf(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.LandsquatAfterDash, _ => YVelocity < 0.5)
            .PermitIf(PlayerFsmTrigger.FaceLedge, PlayerFsmState.DashVault, _ => true, 2)
            .PermitIf(PlayerFsmTrigger.FaceHighLedge, PlayerFsmState.DashVault, _ => true, 10)
            .OnEntry(_ =>
            {

                _playerDashParticles.InvokeDash();
                isSprinting = true;
                IncrementCombo();
                YVelocity = 0;
                YVelocity = Mathf.Max(YVelocity, 15f);
                _dashSinceLeavingGround = true;
                // FMODUnity.RuntimeManager.PlayOneShotAttached(dashFmodEvent, gameObject);
            })
            .OnExit(_ =>
            {
                _momentum = 13f;
                _timeSinceDashFinished = 0f;
            });
    }
}