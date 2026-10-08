public partial class PlayerFsm
{
    private void VaultHangOnUpdate()
    {
        if (UpdateLedgePosition(FaceHighLedgeHeight, true))
        {
            MoveYOntoLedge(VaultHangLedgeYOffset, VaultHangLedgeLerpStrength);
        }
        else
        {
            Machine.Fire(PlayerFsmTrigger.VaultHangFarFromLedge);
        }
        HandleCollisionMove(1f, true, 1f);
        Animator.SetLayerWeight(1, 0);
    }

    private void VaultHangConfigure()
    {
        Machine.Configure(PlayerFsmState.VaultHang)
            .SubstateOf(PlayerFsmState.ForceWallRotation)
            .SubstateOf(GravityFsmState.DontApplyYVelocity)
            // .SubstateOf(GravityFsmState.IgnoreDepenetration)
            .SubstateOf(GravityFsmState.RespectParentTransform)
            // .Permit(PlayerFsmTrigger.VaultHangFarFromLedge, PlayerFsmState.Fall)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.SlowVaultFinish)
            .OnEntry(_ =>
            {
                if (YVelocity < -10f) FMODUnity.RuntimeManager.PlayOneShotAttached(hardlandEventReference, gameObject);
                _arrivedAtLedge = false;
                LastUpwardsY = transform.position.y;
                isSprinting = false;
                EndSurge();
                if (!UpdateLedgePosition(FaceHighLedgeHeight + GetCurrentDashRaycastHeightOffset())) UpdateLedgePosition(FaceLedgeHeight);

            })
            .OnExit(_ =>
            {
                YVelocity = 0;
            });
    }
}