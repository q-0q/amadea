using Code.TriggerParams;
using UnityEngine;
using Wasp;

public partial class PlayerFsm
{
    private void LandableConfigure()
    {
        Machine.Configure(PlayerFsmState.Landable)
            .PermitIf(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.Landsquat, @params =>
            {
                if (YVelocity > 0.5f) return false;
                if (WaterRaycast(out var swimRaycastParam))
                {
                    if (Machine.IsInState(PlayerFsmState.SwimSurfaceRise)) return false;
                    if (IsSwimTrigger(swimRaycastParam)) return false;
                }
                return !IsSlideTrigger(@params);
            })
            .PermitIf(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.Skipsquat,
                _ => Machine.IsInState(PlayerFsmState.FallAfterDash) && _inputBuffer.IsBuffered("Jump"),
                3) // ANTI PATTERN
            .PermitIf(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.HardLand,
                _ =>
                {
                    if (WaterRaycast(out var swimRaycastParam))
                    {
                        if (IsSwimTrigger(swimRaycastParam)) return false;
                    }
                    return CurrentFallDistance() < HardLandAirDiff;
                },
                2)
            // .PermitIf(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.CutsceneHardLand,
            //     _ =>
            //     {
            //         if (WaterRaycast(out var swimRaycastParam))
            //         {
            //             if (IsSwimTrigger(swimRaycastParam)) return false;
            //         }
            //         return CurrentFallDistance() < HardLandAirDiff && CutsceneManager.Singleton.IsCutsceneHardLand();
            //     },
            //     10)
            .PermitIf(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.HardLandRoll,
                _ =>
                {
                    if (WaterRaycast(out var swimRaycastParam))
                    {
                        if (IsSwimTrigger(swimRaycastParam)) return false;
                    }
                    return (CurrentFallDistance() < HardLandAirDiff && _momentum > HardLandRollMinimumMomentum);
                }, 4)
            
            // .PermitIf(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.HardLand,
            //     _ => Machine.IsInState(PlayerFsmState.LongFall), 9)
        // .PermitIf(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.Slide, _ => _slopeTimer > 0.2f, 5)
            .SubstateOf(PlayerFsmState.SlideInteractable)
            .OnExitFrom(GravityFsmTrigger.StartFrameGrounded, @params =>
            {
                if (@params is not RaycastHitParam param) return;
                // print("startframegrounded: " + param.Hit.collider.name );
            })
            .PermitIf(PlayerFsmTrigger.IsAboveLongFall, PlayerFsmState.LongFall, _ =>
            {
                return CurrentFallDistance() < -5f && !Machine.IsInState(PlayerFsmState.DiveFall) && !Machine.IsInState(PlayerFsmState.RopeSwing) && !Machine.IsInState(PlayerFsmState.Swim)  && !Machine.IsInState(PlayerFsmState.Wallrun);
            })
            .PermitIf(PlayerFsmTrigger.SwimTriggerRaycastHit, PlayerFsmState.SwimSurfaceRise, IsSwimTrigger);

    }
    

    
    private bool IsTightropeTrigger(TriggerParams triggerParams)
    {
        if (triggerParams is not RaycastHitParam raycastHitParam) return false;
        return raycastHitParam.kind == GroundKind.Tightrope;
    }
}