using Unity.Mathematics;
using UnityEngine;

public partial class PlayerFsm
{
    private void HardLandRollOnUpdate()
    {
        Animator.SetLayerWeight(1, 0);
        transform.position += ComputeCollisionMove(transform.forward * (HardLandRollForwardSpeed * GetCurrentSurgeSpeedMultiplier() * GetCurrentBoostSpeedMultiplier() * Time.deltaTime));
        HandleTurning(AirControlTurningMultiplier, true, 0, true);
    }

    private void HardLandRollConfigure()
    {
        Machine.Configure(PlayerFsmState.HardLandRoll)
            .SubstateOf(GravityFsmState.Grounded)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.GroundMove)
            .Permit(GravityFsmTrigger.StartFrameAerial, PlayerFsmState.Fall)
            .SubstateOf(PlayerFsmState.TinsicaUsable)
            .PermitIf(GravityFsmTrigger.StartFrameAerial, PlayerFsmState.Jumpsquat, _ => _inputBuffer.IsBuffered("Jump"), 1)
            .OnEntry(_ =>
            {
                FMODUnity.RuntimeManager.PlayOneShotAttached(hardlandEventReference, gameObject);
                FMODUnity.RuntimeManager.PlayOneShotAttached(impactFmodEvent, gameObject);
                OnPlayerFootstep();
                PlaySlipSound();
                LastUpwardsY = transform.position.y;
                _momentum = HardLandRollExitMomentum;
            });
    }
}