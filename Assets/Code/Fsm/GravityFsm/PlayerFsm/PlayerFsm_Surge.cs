using System.Collections;
using Cinemachine;
using Code.Misc;
using Code.TriggerParams;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

public partial class PlayerFsm
{
    private void SurgeStartupOnUpdate()
    {
        // var transposer = _surgeStartupCamera.GetCinemachineComponent<CinemachineTransposer>();
        //
        // var forwardOffset = transform.forward * Mathf.Lerp(0, 4f, Mathf.InverseLerp(0.15f, 0.25f, TimeInCurrentState()));
        // transposer.m_FollowOffset = Vector3.Lerp(transposer.m_FollowOffset,
        //     (transposer.m_FollowOffset.normalized * 14f),  (Time.deltaTime * 0.75f));
        //
        // var composer = _surgeStartupCamera.GetCinemachineComponent<CinemachineComposer>();
        // composer.m_TrackedObjectOffset =
        //     Vector3.Lerp(composer.m_TrackedObjectOffset, forwardOffset, Time.deltaTime * 0.75f);
        //
        //

        var strength = 2.5f;
        var freeLook = PlayerCinemachineFreeLook.Singleton.GetFreeLook();
        freeLook.m_Lens.FieldOfView =
            Mathf.Lerp(freeLook.m_Lens.FieldOfView, 115f, Time.deltaTime * strength);

        var offset = freeLook.transform.GetComponent<CinemachineCameraOffset>();
        offset.m_Offset = Vector3.Lerp(offset.m_Offset,
            new Vector3(0, -1f, 8f), Time.deltaTime * strength);
        
        if (TimeInCurrentState() > 1.25f)
        {
            InteractionCanvas.Singleton.SetPsuedoInteractable("Surge");
            if (_inputBuffer.IsBuffered("Jump"))
            {
                StartCoroutine(SpeedBoostCameraCleanupCoroutine(0.15f));
                Machine.Jump(PlayerFsmState.Idle);
            };
            if (_inputBuffer.IsBuffered("Interact")) Machine.Jump(PlayerFsmState.SurgeDashStartup);
        }
        // if (!_playerInput.actions["Interact"].IsPressed() && TimeInCurrentState() < 0.5f) Machine.Jump(PlayerFsmState.Idle);
        
    }
    
    private void SurgeDashOnUpdate()
    {
        _momentum = MaxMomentum;
        var movementMofifier = Mathf.Lerp(1.35f, 1f, Mathf.InverseLerp(0, 0.3f, TimeInCurrentState()));
        HandleCollisionMove(movementMofifier);
        HandleTurning(0.65f, false, 0f, true, 1f);
    }

    private IEnumerator SpeedBoostCameraCleanupCoroutine(float speedMod = 1f)
    {
        var t = 0f;
        var d = 4.5f * speedMod;
        
        var freeLook = PlayerCinemachineFreeLook.Singleton.GetFreeLook();
        var offset = freeLook.GetComponent<CinemachineCameraOffset>();

        var initialFov = freeLook.m_Lens.FieldOfView;
        var initialOffset = offset.m_Offset;

        var baseFov = PlayerCinemachineFreeLook.Singleton.GetBaseFov();
        while (t < d)
        {

            var w = Util.SmoothLerp01(t / d);
            
            freeLook.m_Lens.FieldOfView =
                Mathf.Lerp(initialFov, baseFov, w);
            
            offset.m_Offset = Vector3.Lerp(initialOffset, Vector3.zero, w);
            t += Time.deltaTime;
            yield return null;
            if (Machine.IsInState(PlayerFsmState.SurgeStartup) || _timeSinceBoostStarted < 0.01f) yield break;
        }

        freeLook.m_Lens.FieldOfView = baseFov;
        offset.m_Offset = Vector3.zero;
    }
    
    
    private void SurgeConfigure()
    {
        Machine.Configure(PlayerFsmState.SurgeStartup)
            .SubstateOf(GravityFsmState.Grounded)
            // .Permit(FsmTrigger.Timeout, PlayerFsmState.SurgeDash)
            .OnExit(_ =>
            {
                InteractionCanvas.Singleton.ClearPsuedoInteractable();
                // _surgeStartupCamera.Priority = -20;
                _surgeStartupCamera.m_Follow = null;
                _surgeStartupCamera.m_LookAt = null;
                surgeStartupFmodInstance.stop(STOP_MODE.ALLOWFADEOUT);
                _inputBuffer.ConsumeBuffer("Jump");
                _currentSurgePedestal.EndChannel();
                _playerSurgeHalo.EndStartup();
            })
            .OnExitFrom(FsmTrigger.Timeout, _ =>
            {
            })
            .OnEntry(@params =>
            {
                _surgeStartupInitialFov = PlayerCinemachineFreeLook.Singleton.GetFreeLook().m_Lens.FieldOfView;
                RuntimeManager.AttachInstanceToGameObject(surgeStartupFmodInstance, gameObject);
                surgeStartupFmodInstance.start();
                
                EndSurge();
                if (@params is SurgePedestalParam surgePedestalParam)
                {
                    _currentSurgePedestal = surgePedestalParam.SurgePedestal;
                    _currentSurgePedestal.StartChannel();
                    _playerSurgeHalo.StartStartup();
                }
                
                _surgeStartupCamera.m_Follow = PlayerCinemachineFreeLook.Singleton.GetFreeLook().m_Follow;
                _surgeStartupCamera.m_LookAt = _surgeStartupCamera.m_Follow;
                
                var state = PlayerCinemachineFreeLook.Singleton.GetFreeLook().State;
                var playerPos = PlayerFsm.Singleton.transform.position;
                
                Vector3 offset = state.RawPosition - playerPos;
                var transposer = _surgeStartupCamera.GetCinemachineComponent<CinemachineTransposer>();
                transposer.m_FollowOffset = offset;
                _surgeStartupCamera.transform.rotation = state.RawOrientation;
                _surgeStartupCamera.m_Lens.FieldOfView = state.Lens.FieldOfView;

                _surgeStartupCamera.GetCinemachineComponent<CinemachineComposer>().m_TrackedObjectOffset = Vector3.zero;
                
                // _surgeStartupCamera.Priority = 20;
                
                Animator.SetLayerWeight(1, 0);
            });
        
        Machine.Configure(PlayerFsmState.SurgeDashStartup)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.SurgeDash)
            .OnExit(_ =>
            {
                StartCoroutine(SpeedBoostCameraCleanupCoroutine());
            })
            .OnEntry(_ =>
            {
                // StartSurge();
            });
        
        Machine.Configure(PlayerFsmState.SurgeDash)
            .SubstateOf(GravityFsmState.Aerial)
            .SubstateOf(PlayerFsmState.WallInteractable)
            .SubstateOf(PlayerFsmState.Landable)
            // .Permit(FsmTrigger.Timeout, PlayerFsmState.GroundMove)
            .PermitIf(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.LandsquatAfterSurge, _ => YVelocity < 0, 1)
            .PermitIf(PlayerFsmTrigger.Dash, PlayerFsmState.Dashsquat, @params => CanDash(@params) && TimeInCurrentState() > 0.3f)
            .OnExit(_ =>
            {
                Shader.SetGlobalFloat("_PlayerTintWeight", 0);
            })
            .OnEntry(_ =>
            {
                _playerSurgeHalo.StartBreak();
                _dashSinceLeavingGround = false;
                
                
                YVelocity = Mathf.Max(YVelocity, 25f);
                _inputBuffer.ConsumeBuffer("Interact");
            });
    }
}