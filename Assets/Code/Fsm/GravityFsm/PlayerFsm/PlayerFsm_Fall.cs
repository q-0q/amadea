public partial class PlayerFsm
{
    private void FallConfigure()
    {
        Machine.Configure(PlayerFsmState.Fall)
            .SubstateOf(GravityFsmState.Aerial)
            .SubstateOf(PlayerFsmState.Landable)
            .SubstateOf(PlayerFsmState.AirControl)
            .SubstateOf(PlayerFsmState.WallInteractable)
            .SubstateOf(PlayerFsmState.PitonInteractable)
            .SubstateOf(PlayerFsmState.RopeSwingInteractable)
            .SubstateOf(PlayerFsmState.MinorLeylineInteractable)
            .SubstateOf(PlayerFsmState.TinsicaUsable)
            .PermitIf(PlayerFsmTrigger.Jump, PlayerFsmState.Jumpsquat, _ => CoyoteTimeClause() && _timeSinceTinsicaExited > CoyoteTime)
            .PermitIf(PlayerFsmTrigger.Jump, PlayerFsmState.TinsicaJump, _ => CoyoteTimeClause() && _timeSinceTinsicaExited < CoyoteTime && PlayerManaManager.Singleton.GetCurrentAvailableMana() >= 1, 2)
            // careful about tinisica buffer permit weight whn adding air tricks in the future
            .Permit(PlayerFsmTrigger.StartUpdraft, PlayerFsmState.Updraft)
            .PermitIf(PlayerFsmTrigger.Attack, PlayerFsmState.ImpaleAir, CanImpale)
            .PermitIf(PlayerFsmTrigger.Attack, PlayerFsmState.GrappleStartup, CanGrapple, 1)
            .PermitIf(PlayerFsmTrigger.IsAboveWater, PlayerFsmState.DiveFall, _ =>
            {
                if (Machine.IsInState(PlayerFsmState.FallAfterDash)) return false;
                return true;
            })
            .PermitIf(PlayerFsmTrigger.Dash, PlayerFsmState.Dashsquat, @params => CanDash(@params) && TimeInCurrentState() > 0.1f); // microfall dash prevention hack.

        Machine.Configure(PlayerFsmState.LongFall)
            .OnEntry(_ =>
            {
                
            })
            .SubstateOf(PlayerFsmState.Fall);

    }

    private bool CoyoteTimeClause()
    {
        return TimeInCurrentState() < CoyoteTime && !Machine.IsInState(PlayerFsmState.FallAfterDash) && !_wallsquattedSinceLeavingGround && !Machine.IsInState(PlayerFsmState.LongFall);
    }
}