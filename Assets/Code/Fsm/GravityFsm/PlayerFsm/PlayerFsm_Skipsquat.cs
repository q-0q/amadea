public partial class PlayerFsm
{
    private void SkipsquatOnUpdate()
    {
        SetSafeGroundPosition();
        HandleCollisionMove(0.25f, false);
    }
    
    private void SkipsquatConfigure()
    {
        Machine.Configure(PlayerFsmState.Skipsquat)
            .SubstateOf(GravityFsmState.Grounded)
            // .SubstateOf(PlayerFsmState.TinsicaUsable)
            // .PermitIf(PlayerFsmTrigger.FaceLedge, PlayerFsmState.Vault, _ => true)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.Skip)
            .OnEntry(_ =>
            {
                _wallsquattedSinceLeavingGround = false;
                _dashSinceLeavingGround = false;
                _previousWallrunSide = FlankType.None;
                _currentFlankType = FlankType.None;
                currentRopeSwing = null;
                LastUpwardsY = transform.position.y;
                OnPlayerFootstep();
                PlaySlipSound();
                DeathGroundImmuneOneshotHandler();
            })
            .OnExit(_ =>
            {
                _timeSinceSkipsquatExited = 0f;
            });
    }
}