using UnityEngine;

public partial class PlayerFsm
{
    private void SlowVaultFinishOnUpdate()
    {
        HandleTurning(VaultTurningMultiplier, true);
        bool setLedgePosition = false;
        
        
        if (!UpdateLedgePosition(FaceHighLedgeHeight, true))
        {
            setLedgePosition = UpdateLedgePosition(0f);
        }
        else
        {
            setLedgePosition = true;
        }
        if (setLedgePosition)
            MoveYOntoLedge(0f, SlowVaultFinishLedgeLerpStrength);
        
        
        
        transform.position += transform.forward * (SlowVaultFinishForwardSpeed * Time.deltaTime);
    }

    private void SlowVaultFinishConfigure()
    {
        Machine.Configure(PlayerFsmState.SlowVaultFinish)
            .SubstateOf(PlayerFsmState.ForceWallRotation)
            .SubstateOf(GravityFsmState.DontApplyYVelocity)
            .SubstateOf(PlayerFsmState.TinsicaUsable)
            .SubstateOf(GravityFsmState.RespectParentTransform)
            // .SubstateOf(GravityFsmState.IgnoreDepenetration)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.Idle)
            .PermitIf(FsmTrigger.Timeout, PlayerFsmState.GroundMove, _ =>
            {
                if (PhotoManager.Singleton.IsActive()) return false;
                var v2 = GetInputMovementVector2();
                return v2.magnitude > InputMagnitudeThreshhold;
            }, 2)
            // .Permit(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.GroundMove)
            .OnEntry(_ =>
            {
                _inputBuffer.ConsumeBuffer("Jump");
                YVelocity = 0;
                OnPlayerFootstep();
                _wallsquattedSinceLeavingGround = false;
                _dashSinceLeavingGround = false;
                _previousWallrunSide = FlankType.None;
                _currentFlankType = FlankType.None;
                _wallsquattedSinceLeavingGround = false;
                _dashSinceLeavingGround = false;
                currentRopeSwing = null;
                
            })
            .OnExit(_ => { 
                _momentum = 5f;
                // SnapToGround();
            });
    }
}