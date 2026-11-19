using UnityEngine;

public partial class PlayerFsm
{
    private void HardLandOnUpdate()
    {
        Animator.SetLayerWeight(1, 0);
        if (TimeInCurrentState() < HardLandForwardDuration)
        {
            transform.position += ComputeCollisionMove(transform.forward * (HardLandForwardSpeed * Time.deltaTime));
        }
    }
    private void HardLandConfigure()
    {
        Machine.Configure(PlayerFsmState.HardLand)
            .SubstateOf(GravityFsmState.Grounded)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.GroundMove)
            .OnEntry(@params =>
            {
                isSprinting = false;
                EndSurge();
                FMODUnity.RuntimeManager.PlayOneShotAttached(impactFmodEvent, gameObject);
                FMODUnity.RuntimeManager.PlayOneShotAttached(hardlandEventReference, gameObject);
                OnPlayerFootstep();
                PlaySlipSound();
                _momentum = HardLandExitMomentum;

                if (@params is RaycastHitParam raycastHitParam)
                {
                    SetParentTransform(raycastHitParam.Hit.transform);
                }
            });

        Machine.Configure(PlayerFsmState.CutsceneHardLand)
            .SubstateOf(PlayerFsmState.HardLand)
            .PermitIf(FsmTrigger.Timeout, PlayerFsmState.Idle, _ => true, 2)
            .OnEntry(_ =>
            {
                FMODUnity.RuntimeManager.PlayOneShotAttached(hardlandCinematicEventReference, gameObject);
            });
    }
}