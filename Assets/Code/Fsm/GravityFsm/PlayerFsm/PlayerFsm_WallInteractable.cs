using System;
using UnityEngine;
using Wasp;

public partial class PlayerFsm
{
    private void WallInteractableConfigure()
    {
        Machine.Configure(PlayerFsmState.WallInteractable)
            .PermitIf(PlayerFsmTrigger.FaceLedge, PlayerFsmState.Vault, CanVault, 1)
            .PermitIf(PlayerFsmTrigger.FaceLedge, PlayerFsmState.MediumVaultHang, _ =>
            {
                if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out var hit, 5f, GetEnvironmentalLayermask()))
                {
                    var deltaY = hit.point.y - transform.position.y;
                    if (deltaY > -2f) return false;
                }
                
                return (!Machine.IsInState(PlayerFsmState.PitonFlip) ||
                        YVelocity < PitonMaximumWallInteractYVelocity) &&
                       !CutsceneManager.Singleton.IsCutscenePlayerDisabled() && _timeSinceMinorLeyline > 0.2f;
            })
            .PermitIf(PlayerFsmTrigger.FaceWall, PlayerFsmState.Wallsquat,
                _ => _momentum > WallSquatMinimumMomentum && WallsquatVelocityChecker() && !_wallsquattedSinceLeavingGround)
            .PermitIf(PlayerFsmTrigger.FaceWallStrict, PlayerFsmState.Wallsquat,
                _ => _momentum > WallSquatMinimumMomentum && WallsquatVelocityChecker() && !_wallsquattedSinceLeavingGround)
            .PermitIf(PlayerFsmTrigger.FaceHighLedge, PlayerFsmState.Wallsquat,
                _ => _momentum > WallSquatMinimumMomentum && WallsquatVelocityChecker() && !_wallsquattedSinceLeavingGround)
            .PermitIf(PlayerFsmTrigger.FlankWall, PlayerFsmState.Wallrun,
                _ => WallrunVelocityChecker());
    }

    private bool CanVault(TriggerParams t)
    {
        if (CutsceneManager.Singleton.IsCutscenePlayerDisabled()) return false;
        if (Machine.IsInState(PlayerFsmState.PitonFlip) && YVelocity > PitonMaximumWallInteractYVelocity) return false;
        if (Machine.IsInState(PlayerFsmState.GroundMove)) return true;
        return (YVelocity > VaultMinimumYVelocity && _momentum > VaultMinimumMomentum && _timeSinceMinorLeyline > 0.2f);
    }

    private bool WallsquatVelocityChecker()
    {
        if (Machine.IsInState(PlayerFsmState.PitonFlip)) return YVelocity < PitonMaximumWallInteractYVelocity;
        return YVelocity < WallsquatMinimumYVelocity || IsInGust && _timeSinceMinorLeyline > 0.2f;
    }
    
    private bool WallrunVelocityChecker()
    {
        return isSprinting && _momentum > WallRunMinimumMomentum && (YVelocity < WallRunMinimumYVelocity || IsInGust);
    }
}