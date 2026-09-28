public partial class PlayerFsm
{
    private void MediumVaultHangConfigure()
    {
        
        Machine.Configure(PlayerFsmState.MediumVaultHang)
            .SubstateOf(PlayerFsmState.VaultHang)
            .OnEntry(_ =>
            {
                FMODUnity.RuntimeManager.PlayOneShotAttached(impactFmodEvent, gameObject);
                OnPlayerFootstep();
                PlaySlipSound();
                _momentum = 12f;
            });
    }
}