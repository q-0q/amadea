public partial class PlayerFsm
{

    private void JumpOnUpdate()
    {
        
    }
    
    private void JumpConfigure()
    {
        Machine.Configure(PlayerFsmState.Jump)
            .SubstateOf(GravityFsmState.Aerial)
            .SubstateOf(PlayerFsmState.Landable)
            .SubstateOf(PlayerFsmState.AirControl)
            .SubstateOf(PlayerFsmState.WallInteractable)
            .SubstateOf(PlayerFsmState.PitonInteractable)
            .SubstateOf(PlayerFsmState.RopeSwingInteractable)
            .SubstateOf(PlayerFsmState.MinorLeylineInteractable)
            // .SubstateOf(PlayerFsmState.DolphinUsable)
            .PermitIf(PlayerFsmTrigger.Attack, PlayerFsmState.ImpaleAir, CanImpale)
            .PermitIf(PlayerFsmTrigger.Attack, PlayerFsmState.GrappleStartup, CanGrapple, 1)
            .Permit(PlayerFsmTrigger.StartUpdraft, PlayerFsmState.Updraft)
            .Permit(PlayerFsmTrigger.IsAboveWater, PlayerFsmState.DiveFall)
            .PermitIf(PlayerFsmTrigger.Dash, PlayerFsmState.Dashsquat, CanDash)
            .OnEntryFrom(PlayerFsmTrigger.Jump, _ =>
            {
                OnPlayerFootstep();
                YVelocity = JumpYVelocity; 
            });
    }
}