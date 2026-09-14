using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;

public partial class PlayerFsm
{
    private void VaultOnUpdate()
    {
        _momentum = Mathf.Max(_momentum, VaultMinimumMomentumOnUpdate);
        var momentumWeight = ComputeMomentumWeight();
        Animator.SetFloat("SpeedMod",
            Mathf.Lerp(VaultMinimumAnimatorSpeedMod, VaultMaximumAnimatorSpeedMod, momentumWeight));
        var dashVault = Machine.IsInState(PlayerFsmState.DashVault);
        if (UpdateLedgePosition(FaceHighLedgeHeight))
        {
            MoveYOntoLedge(0f, dashVault? VaultLedgeLerpStrength : VaultLedgeLerpStrength);
        }
        SetAnimatorMomentum();
        var movementModifier = dashVault ? 0.9f : 0.9f;
        transform.position += ComputeCollisionMove(ComputeDesiredMove()) * movementModifier;
        HandleTurning(VaultTurningMultiplier, true);
    }

    private void VaultConfigure()
    {
        Machine.Configure(PlayerFsmState.Vault)
            .SubstateOf(GravityFsmState.DontApplyYVelocity)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.GroundMove)
            .SubstateOf(GravityFsmState.RespectParentTransform)
            // .Permit(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.Landsquat)
            .PermitIf(FsmTrigger.Timeout, PlayerFsmState.Jumpsquat, _ => _inputBuffer.IsBuffered("Jump"), 1)
            .OnEntry(_ =>
            {
                _movementAnimationMirror = !_movementAnimationMirror;
                var flip = _movementAnimationMirror ? 0 : 1f;
                Animator.SetFloat("Flip", flip);
                UpdateLedgePosition(FaceLedgeHeight);
                YVelocity = 0;
                Animator.SetLayerWeight(3, 0);
                _arrivedAtLedge = false;
                _wallsquattedSinceLeavingGround = false;
                _dashSinceLeavingGround = false;
                _previousWallrunSide = FlankType.None;
                _currentFlankType = FlankType.None;
                currentRopeSwing = null;
                
                FMODUnity.RuntimeManager.PlayOneShotAttached(jumpFmodEvent, gameObject);
                OnPlayerFootstep();
                
                IncrementCombo();
            })
            .OnExit(_ =>
            {
                _momentum = Mathf.Min(MaxMomentum, _momentum + 2f);
            });
        
        Machine.Configure(PlayerFsmState.DashVault)
            .PermitIf(FsmTrigger.Timeout, PlayerFsmState.Skip, _ => _inputBuffer.IsBuffered("Jump", 0.25f), 2)
            // .PermitIf(FsmTrigger.Timeout, PlayerFsmState.Skip, _ => _inputBuffer.IsBuffered("Jump", 0.25f), 2)
            .SubstateOf(PlayerFsmState.TinsicaUsable)
            .SubstateOf(PlayerFsmState.Vault)
            .OnEntry(_ =>
            {
                _currentLedgePosition = transform.position;
            })
            .OnExit(_ =>
            {
                _momentum = MaxMomentum;
                isSprinting = true;
                
                // var originYOffset = -0.1f;
                // var originForwardOffset = -2f;
                // var origin = transform.position + (Vector3.up * originYOffset) + (transform.forward * originForwardOffset);
                // if (Physics.Raycast(origin, transform.forward, out var hit, 10f, GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore))
                // {
                //     Debug.DrawLine(origin, hit.point, Color.green, 5f);
                //     transform.position += transform.forward*  (hit.distance + 0.05f);
                // }
            });
    }
    
    
}