using UnityEngine;

public partial class PlayerFsm
{

    private void SkipOnUpdate()
    {
        Animator.SetLayerWeight(1, 0);
        transform.position += ComputeCollisionMove(transform.forward * (Time.deltaTime * SkipForwardBonusSpeed));
    }
    
    private void SkipConfigure()
    {
        Machine.Configure(PlayerFsmState.Skip)
            .SubstateOf(GravityFsmState.Aerial)
            .SubstateOf(PlayerFsmState.Landable)
            .SubstateOf(PlayerFsmState.AirControl)
            .SubstateOf(PlayerFsmState.WallInteractable)
            .SubstateOf(PlayerFsmState.PitonInteractable)
            .SubstateOf(PlayerFsmState.RopeSwingInteractable)
            .PermitIf(PlayerFsmTrigger.StartUpdraft, PlayerFsmState.Updraft, _ => TimeInCurrentState() > 0.35f)
            .PermitIf(PlayerFsmTrigger.Attack, PlayerFsmState.ImpaleAir, CanImpale)
            .PermitIf(PlayerFsmTrigger.Attack, PlayerFsmState.GrappleStartup, CanGrapple, 1)
            // .PermitIf(PlayerFsmTrigger.IsAboveWater, PlayerFsmState.DiveFall, _ => TimeInCurrentState() > 0.4f)
            .PermitIf(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.LandsquatAfterDash, @params => !IsSlideTrigger(@params) && YVelocity < 0.5f && _momentum > 5f, 1)
            .OnEntry(_ =>
            {
                _playerDashParticles.InvokeSkip();
                FMODUnity.RuntimeManager.PlayOneShotAttached(skipWhooshEventReference, gameObject);
                _momentum = 13f;
                IncrementCombo();
                _inputBuffer.ConsumeBuffer("Jump");
                PlaySlipSound();
                FMODUnity.RuntimeManager.PlayOneShotAttached(skipFmodEvent, gameObject);
            })
            .OnEntryFrom(FsmTrigger.Timeout, _ => { YVelocity = SkipYVelocity; });
        
        // TODO: skip above water
    }
}