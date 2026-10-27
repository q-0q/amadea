public partial class PlayerFsm
{

    private void FallAfterDashOnUpdate()
    {
        Animator.SetLayerWeight(1, 0);
    }
    
    private void FallAfterDashConfigure()
    {
        Machine.Configure(PlayerFsmState.FallAfterDash)
            .SubstateOf(PlayerFsmState.Fall)
            .PermitIf(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.LandsquatAfterDash, _ => true, 1)
            .SubstateOf(PlayerFsmState.WallInteractable)
            .PermitIf(PlayerFsmTrigger.FaceLedge, PlayerFsmState.DashVault, _ => true, 2)
            .PermitIf(PlayerFsmTrigger.FaceHighLedge, PlayerFsmState.DashVault, _ => true, 10)
            .OnExit(_ =>
            {
                _timeSinceDashFinished = 0f;
            });
    }
}