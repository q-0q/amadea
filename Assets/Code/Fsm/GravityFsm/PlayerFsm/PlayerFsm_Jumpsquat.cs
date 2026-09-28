public partial class PlayerFsm
{
    private void JumpsquatConfigure()
    {
        Machine.Configure(PlayerFsmState.Jumpsquat)
            .SubstateOf(GravityFsmState.Grounded)
            .SubstateOf(PlayerFsmState.LockMomentum)
            // .PermitIf(PlayerFsmTrigger.FaceLedge, PlayerFsmState.Vault, _ => true)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.Jump)
            .OnEntry(_ =>
            {
                Animator.SetLayerWeight(1, 0);
                _inputBuffer.ConsumeBuffer("Jump");
                FMODUnity.RuntimeManager.PlayOneShotAttached(jumpFmodEvent, gameObject);
                OnPlayerFootstep();
                
                PlaySlipSound();

            })
            .OnExitFrom(FsmTrigger.Timeout, _ =>
            {
                OnPlayerFootstep();
                YVelocity = JumpYVelocity; 

            });

        Machine.Configure(PlayerFsmState.WallrunJumpsquat)
            .SubstateOf(PlayerFsmState.Jumpsquat);

    }
}