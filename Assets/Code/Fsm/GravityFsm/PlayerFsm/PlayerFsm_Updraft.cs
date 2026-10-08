using System;
using System.Collections;
using UnityEngine;

public partial class PlayerFsm
{

    public static event Action OnPlayerEnterUpdraft;
    private void UpdraftOnUpdate()
    {
        
        var bobOffset = Mathf.Lerp(5, -5f, Mathf.InverseLerp(-1f, 1f, Mathf.Sin(Time.time * 2.5f)));

        float targetYVelocity = _playerInput.actions["Jump"].IsPressed() ? 20f : -30f;
        targetYVelocity = Mathf.Lerp(targetYVelocity, -30f,
            Mathf.InverseLerp(UpdraftDuration - 2f, UpdraftDuration, TimeInCurrentState()));
        
        targetYVelocity = Mathf.Lerp(40f, targetYVelocity, Mathf.InverseLerp(0f, 1f, TimeInCurrentState()));
        float lerpStrength = targetYVelocity > YVelocity ? 8f : 3f;
        YVelocity = Mathf.Lerp(YVelocity, targetYVelocity + bobOffset, Time.deltaTime * lerpStrength);
        
        
        var tintWeight = Mathf.InverseLerp(7f, 15f, YVelocity) * 0.65f;
        tintWeight = Mathf.Lerp(tintWeight, 0f,
            Mathf.InverseLerp(UpdraftDuration - 1f, UpdraftDuration, TimeInCurrentState()));
        Shader.SetGlobalFloat("_PlayerTintWeight", tintWeight);
        
        if (TimeInCurrentState() > 0.45f)
        {
            _dashSinceLeavingGround = false;
            _wallsquattedSinceLeavingGround = false;
        }
        Animator.SetFloat("UpdraftAmount", YVelocity);



        var multiplier = Mathf.Lerp(2f, 1.65f, Mathf.InverseLerp(2f, 30f, YVelocity));
        multiplier = Mathf.Lerp(multiplier, 1f,
            Mathf.InverseLerp(UpdraftDuration - 1f, UpdraftDuration, TimeInCurrentState()));
        
        HandleCollisionMove(multiplier);
    }
    private void UpdraftConfigure()
    {
        Machine.Configure(PlayerFsmState.Updraft)
            .SubstateOf(GravityFsmState.Aerial)
            .SubstateOf(GravityFsmState.DontLoseYVelocity)
            .SubstateOf(PlayerFsmState.AirControl)
            .SubstateOf(PlayerFsmState.WallInteractable)
            .SubstateOf(PlayerFsmState.Landable)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.Fall)
            .PermitIf(PlayerFsmTrigger.Dash, PlayerFsmState.Dashsquat, CanDash)
            .OnEntry(_ =>
            {
                // _movementAnimationMirror = !_movementAnimationMirror;
                // var flip = _movementAnimationMirror ? 0 : 1f;
                // Animator.SetFloat("Flip", flip);
                LastUpwardsY = transform.position.y;
                OnPlayerEnterUpdraft?.Invoke();
                _previousWallrunSide = FlankType.None;
                _currentFlankType = FlankType.None;
                currentRopeSwing = null;
                _inputBuffer.ConsumeBuffer("Dash");
                

                // StartCoroutine(Coroutine());


                IEnumerator Coroutine()
                {
                    var t = 0f;
                    var d = 0.5f;

                    while (t < d)
                    {
                        var w = t / d;
                        Shader.SetGlobalFloat("_PlayerTintWeight", 1f - w);
                        t += Time.deltaTime;
                        yield return null;
                    }
                    
                    Shader.SetGlobalFloat("_PlayerTintWeight", 0f);
                }
            })
            .OnExit(_ =>
            {
                Shader.SetGlobalFloat("_PlayerTintWeight", 0f);
            });
    }
}