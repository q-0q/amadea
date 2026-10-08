using Code.TriggerParams;
using UnityEngine;

public partial class PlayerFsm
{
    private void WalkToPositionOnUpdate()
    {
        var toTarget = walkToPositionTarget - transform.position;
        toTarget = new Vector3(toTarget.x, 0, toTarget.z);
        var angle = Vector3.Angle(transform.forward, toTarget);
        var isInTurnPhase = angle > WalkToPositionTurnPhaseAngle;
        _momentum = Mathf.Lerp(_momentum, isInTurnPhase ? 0 : WalkToPositionMomentum, Time.deltaTime * WalkToPositionMomentumLerpStrength);
        HandleTurningCore(1f, 0f, toTarget);
        HandleCollisionMove();
        SetAnimatorMomentum();
        SetAnimatorSpeedMod();
    }

    private void SetAnimatorSpeedMod()
    {
        var speedMod = Mathf.Lerp(GroundMoveMinimumAnimatorSpeedMod, GroundMoveMaximumAnimatorSpeedMod, ComputeMomentumWeight()) *
                       (_isSurging ? 1.5f : 1f) * GetCurrentBoostSpeedMultiplier();
        Animator.SetFloat("SpeedMod", speedMod);
    }

    private void WalkToPositionConfigure()
    {
        Machine.Configure(PlayerFsmState.WalkToPosition)
            .SubstateOf(GravityFsmState.Grounded)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.Idle)
            .OnEntry(param =>
            {
                if (param is not InteractableParam interactionParam) return;
                walkToPositionTarget = interactionParam.WalkToPositionTarget;
                walkToPositionArrivalDistanceModifier = interactionParam.Interactable.arrivalDistanceModifier;
            });
        
        Machine.Configure(PlayerFsmState.WalkToTerminalNodePosition)
            .SubstateOf(PlayerFsmState.WalkToPosition)
            .Permit(PlayerFsmTrigger.ArriveAtWalkToPositionTarget, PlayerFsmState.TerminalNodeInteract);
        
        Machine.Configure(PlayerFsmState.WalkToRotationDaisPosition)
            .SubstateOf(PlayerFsmState.WalkToPosition)
            .Permit(PlayerFsmTrigger.ArriveAtWalkToPositionTarget, PlayerFsmState.RotationDaisInteract);
        
        Machine.Configure(PlayerFsmState.WalkToChestPosition)
            .SubstateOf(PlayerFsmState.WalkToPosition)
            .Permit(PlayerFsmTrigger.ArriveAtWalkToPositionTarget, PlayerFsmState.ChestInteract);
        
        Machine.Configure(PlayerFsmState.WalkToTravelPosition)
            .SubstateOf(PlayerFsmState.WalkToPosition)
            .Permit(PlayerFsmTrigger.ArriveAtWalkToPositionTarget, PlayerFsmState.Travel);

        Machine.Configure(PlayerFsmState.Travel)
            .SubstateOf(GravityFsmState.RespectParentTransform)
            .SubstateOf(GravityFsmState.IgnoreCollisionMoveClipping)
            .SubstateOf(GravityFsmState.IgnoreDepenetration);

        Machine.Configure(PlayerFsmState.RotationDaisInteract)
            .SubstateOf(PlayerFsmState.DisplaceFoliage)
            .SubstateOf(PlayerFsmState.Interactable);

        Machine.Configure(PlayerFsmState.ChestInteract)
            .SubstateOf(PlayerFsmState.DisplaceFoliage)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.IdleLong);
        
        
        Machine.Configure(PlayerFsmState.WalkToGenericInteractable)
            .SubstateOf(PlayerFsmState.WalkToPosition)
            .Permit(PlayerFsmTrigger.ArriveAtWalkToPositionTarget, PlayerFsmState.GenericInteract);
        
        
        Machine.Configure(PlayerFsmState.GenericInteract)
            .SubstateOf(PlayerFsmState.DisplaceFoliage)
            .SubstateOf(PlayerFsmState.Interactable)
            .OnEntry(_ =>
            {
                if (currentInteractable != null) currentInteractable.TriggerHardInteraction();
            });
    }
    
    private void WalkToSwitchPositionConfigure()
    {
        Machine.Configure(PlayerFsmState.WalkToSwitchPosition)
            .SubstateOf(PlayerFsmState.WalkToPosition)
            .Permit(PlayerFsmTrigger.ArriveAtWalkToPositionTarget, PlayerFsmState.InteractWithSwitch);
    }
    
    private void WalkToDialoguePositionConfigure()
    {
        Machine.Configure(PlayerFsmState.WalkToDialoguePosition)
            .SubstateOf(PlayerFsmState.WalkToPosition)
            .Permit(PlayerFsmTrigger.ArriveAtWalkToPositionTargetRanged, PlayerFsmState.Dialogue);
    }
}