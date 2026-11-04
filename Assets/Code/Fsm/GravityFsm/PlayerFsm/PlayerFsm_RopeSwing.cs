using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Wasp;

public partial class PlayerFsm
{

    private void RopeSwingHomingOnUpdate()
    {
        
        Animator.SetFloat("RopeSwingDirection", Mathf.Lerp(0, 0.1f, Mathf.InverseLerp(0, 0.15f, TimeInCurrentState())));
        
        HandleTurning(0f, false, 1f, false, isSprinting ? 0.5f : 1f);
        var desiredPosition = currentRopeSwing.GetWorldspaceAttachPoint();
        transform.position = Vector3.Lerp(transform.position, desiredPosition,
            Time.deltaTime * Mathf.Lerp(1.5f, 1f, Mathf.InverseLerp(0, 0.2f, TimeInCurrentState())));
        
        if (Vector3.SqrMagnitude(transform.position - desiredPosition) < 0.1f) Machine.Jump(PlayerFsmState.RopeSwing);
    }
    
    private void RopeSwingOnUpdate()
    {
        // if (_playerInput.actions["Sprint"].IsPressed()) isSprinting = true;
        
        // HandleInputMomentumChange();
        
        var v3 = PhotoManager.Singleton.IsActive() ? Vector3.zero : GetInputMovementVector3();
        var angle = Vector3.Angle(transform.forward, v3);
        var turnStrength = Mathf.Lerp(0, 0.175f, Mathf.InverseLerp(45f, 30f, Mathf.Abs(angle - 90)));
        HandleTurning(turnStrength, true, 1f, false, 0f);
        
        // SetAnimatorMomentum();
        // SetAnimatorSpeedMod();

        var desiredPosition = currentRopeSwing.GetWorldspaceAttachPoint();
        transform.position = Vector3.Lerp(transform.position, desiredPosition,
            Time.deltaTime * Mathf.Lerp(8f, 60f, Mathf.InverseLerp(0, 0.5f, TimeInCurrentState())));
        
        var dot = Vector3.Dot(transform.forward, currentRopeSwing.GetWorldAcceleration());
        var swingAmount = Mathf.InverseLerp(50f, -40f, dot);
        Animator.SetFloat("RopeSwingDirection", Mathf.Lerp(Animator.GetFloat("RopeSwingDirection"), swingAmount, Time.deltaTime * 20f));
        
        
        currentRopeSwing.SetWorldPlayerInput(GetInputMovementVector3());
        
    }

    private void RopeSwingJumpsquatOnUpdate()
    {
        HandleCollisionMove(0.5f);

    }
    
    private void RopeSwingConfigure()
    {

        Machine.Configure(PlayerFsmState.RopeSwingInteractable)
            .PermitIf(PlayerFsmTrigger.EnterRopeSwingTrigger, PlayerFsmState.RopeSwingHoming, @params =>
            {
                if (@params is not RopeSwingHitParam ropeSwingHitParam) return false;
                if (ropeSwingHitParam.RopeSwing == currentRopeSwing) return TimeInCurrentState() > 0.6f;
                return true;
            });

        Machine.Configure(PlayerFsmState.RopeSwingHoming)
            .SubstateOf(GravityFsmState.Aerial)
            .SubstateOf(PlayerFsmState.ForceWallRotation)
            .SubstateOf(GravityFsmState.DontApplyYVelocity)
            .SubstateOf(GravityFsmState.RespectParentTransform)
            .SubstateOf(GravityFsmState.IgnoreDepenetration)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.RopeSwing)
            .OnEntry(_ =>
            {
                // _momentum = 0;
                _wallsquattedSinceLeavingGround = false;
                _dashSinceLeavingGround = false;
                _previousWallrunSide = FlankType.None;
                _currentFlankType = FlankType.None;
                // currentRopeSwing = null;
                Animator.SetFloat("RopeSwingDirection", 0);
            })
            .OnEntryFrom(PlayerFsmTrigger.EnterRopeSwingTrigger, @params =>
            {
                if (@params is not RopeSwingHitParam ropeSwingHitParam) return;
                currentRopeSwing = ropeSwingHitParam.RopeSwing;
                currentRopeSwing.SetPlayerPosition(transform.position);
            });

        Machine.Configure(PlayerFsmState.RopeSwing)
            .SubstateOf(GravityFsmState.Aerial)
            .SubstateOf(PlayerFsmState.Landable)
            .SubstateOf(GravityFsmState.DontApplyYVelocity)
            .SubstateOf(GravityFsmState.DontLoseYVelocity)
            .PermitIf(PlayerFsmTrigger.Jump, PlayerFsmState.RopeSwingJumpsquat, _ => TimeInCurrentState() > 0.1f)
            .PermitIf(PlayerFsmTrigger.FaceLedge, PlayerFsmState.Vault, CanVault, 1)
            .PermitIf(PlayerFsmTrigger.FaceLedge, PlayerFsmState.MediumVaultHang, _ => !Machine.IsInState(PlayerFsmState.PitonFlip) || YVelocity < PitonMaximumWallInteractYVelocity)
            // .PermitIf(PlayerFsmTrigger.FaceWall, PlayerFsmState.Wallsquat,
            //     _ => _momentum > WallSquatMinimumMomentum && WallsquatVelocityChecker() && !_wallsquattedSinceLeavingGround)
            // .PermitIf(PlayerFsmTrigger.FaceWallStrict, PlayerFsmState.Wallsquat,
            //     _ => _momentum > WallSquatMinimumMomentum && WallsquatVelocityChecker() && !_wallsquattedSinceLeavingGround)
            // .PermitIf(PlayerFsmTrigger.FaceHighLedge, PlayerFsmState.Wallsquat,
            //     _ => _momentum > WallSquatMinimumMomentum && WallsquatVelocityChecker() && !_wallsquattedSinceLeavingGround)
            .OnEntry(_ =>
            {
                currentRopeSwing.SetPlayerMomentum(_momentum);
            })
            .OnExitFrom(PlayerFsmTrigger.Jump, _ =>
            {
                transform.forward = new Vector3(currentRopeSwing.GetWorldSwingDirection().x, 0, currentRopeSwing.GetWorldSwingDirection().z).normalized;
                _momentum = MaxMomentum;
            })
            .OnExit(_ =>
            {
                LastUpwardsPosition = transform.position;
                _timeSinceRopeSwing = 0;
                currentRopeSwing.SetWorldPlayerInput(Vector3.zero);
            });

        Machine.Configure(PlayerFsmState.RopeSwingJumpsquat)
            .PermitIf(FsmTrigger.Timeout, PlayerFsmState.RopeSwingJump, _ => true, 2)
            .OnEntry(_ =>
            {
                Animator.SetLayerWeight(1, 0);
                _inputBuffer.ConsumeBuffer("Jump");
            })
            .OnExitFrom(FsmTrigger.Timeout, _ =>
            {
                YVelocity = JumpYVelocity; 
                Animator.SetLayerWeight(1, 0);
            });

        Machine.Configure(PlayerFsmState.RopeSwingJump)
            .SubstateOf(PlayerFsmState.Jump);

    }
    
}