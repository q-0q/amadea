using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{

    private float YLerpRate = 2.75f;

    public static event Action<CameraBehaviorZone> OnCameraFollowTriggerStay;
    private float _currentYOffset = 0;

    private float _currentXZLerp;
    private float _currentYLerp;

    private Vector3 _currentDialogueOffset;

    private CinemachineBrain _brain;
    private CinemachineFreeLook _freeLook;
    
    private Vector3 _nonCutsceneLocation;

    private void Start()
    {
        transform.position = PlayerFsm.Singleton.transform.position;
        transform.rotation = PlayerFsm.Singleton.transform.rotation;
        _currentXZLerp = 100f;
        _currentYLerp = YLerpRate;
        Shader.SetGlobalVector("_CameraFollowWorldPosition", transform.position);
        _brain = Camera.main.GetComponent<CinemachineBrain>();
        _freeLook = PlayerCinemachineFreeLook.Singleton.GetFreeLook();
    }

    private void OnTriggerStay(Collider other)
    {
        // other.transform.TryGetComponent(out CameraBehaviorZone cameraBehaviorZone);
        // OnCameraFollowTriggerStay?.Invoke(cameraBehaviorZone);
        
    }
    
    private void OnTriggerExit(Collider other)
    {
        OnCameraFollowTriggerStay?.Invoke(null);
    }

    private void OnEnable()
    {
        TestCutsceneFsm.OnIntroCutsceneGondolaTeleported += OnWarp;
        PlayerFsm.OnPlayerTeleported += OnWarp;
        PlayerFsm.OnPlayerRespawn += OnRespawn;
        CutsceneManager.OnOverwriteCameraFollowEnded += OnShaderPositionReset;
    }

    private void OnRespawn()
    {
        transform.position = PlayerFsm.Singleton.transform.position;
    }

    private void OnDisable()
    {
        TestCutsceneFsm.OnIntroCutsceneGondolaTeleported -= OnWarp;
        PlayerFsm.OnPlayerTeleported -= OnWarp;
        PlayerFsm.OnPlayerRespawn -= OnRespawn;
        CutsceneManager.OnOverwriteCameraFollowEnded -= OnShaderPositionReset;
    }

    private void OnWarp(Vector3 delta)
    {
        transform.position += delta;
        Shader.SetGlobalVector("_CameraFollowWorldPosition", transform.position);
    }
    
    private void LateUpdate()
    {
        // if (PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.TrialTeleport))
        // {
        //     if (PlayerFsm.Singleton.TimeInCurrentState() > 0.5f)
        //     {
        //         transform.position = Vector3.Lerp(transform.position, PlayerFsm.Singleton.GetTeleportDestination(),
        //             Time.deltaTime * 3f);
        //         return;
        //     }
        // }

        if (CutsceneManager.Singleton.IsCutsceneOverwriteCameraFollowShaderPosition(out var t) && _freeLook.gameObject != _brain.ActiveVirtualCamera.VirtualCameraGameObject)
        {
            Shader.SetGlobalVector("_CameraFollowWorldPosition",  t == null ? Camera.main.transform.position : t.position);
        }
        else
        {
            var shaderPositionXZLerpSpeed = 10f;
            var shaderPositionYLerpSpeed =
                Mathf.Lerp(1.75f, 10f, Mathf.InverseLerp(-15f, -30f, PlayerFsm.Singleton.GetYVelocity()));
            
            var currentShaderValue = Shader.GetGlobalVector("_CameraFollowWorldPosition");
            var newShaderX = Mathf.Lerp(currentShaderValue.x, transform.position.x,
                Time.deltaTime * shaderPositionXZLerpSpeed);
            var newShaderY = Mathf.Lerp(currentShaderValue.y, transform.position.y,
                Time.deltaTime * shaderPositionYLerpSpeed);
            var newShaderZ = Mathf.Lerp(currentShaderValue.z, transform.position.z,
                Time.deltaTime * shaderPositionXZLerpSpeed);
            Shader.SetGlobalVector("_CameraFollowWorldPosition", new Vector3(newShaderX, newShaderY, newShaderZ));
        }


        // if (PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.Dead) || PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.Dying1)) return;
        
        var pos = CameraFollowTarget.Singleton.transform.position;
        var yLerp = PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.CutsceneWary)
            ? YLerpRate * 4f
            : YLerpRate;

        var xzLerp = 100f;
        var newYOffset = 0f;
        
        if (PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.Slide) || PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.FallAfterSlideLateral))
        {
            newYOffset = -5f;
        }
        
        if (PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.RopeSwing) || PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.RopeSwingHoming))
        {
            yLerp *= 0.15f;
            xzLerp *= 0.15f;
            newYOffset = -1f;
        }
        
        if (PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.SurgeDash))
        {
            xzLerp *= Mathf.Lerp(0.15f, 1f, Mathf.InverseLerp(0f, 0.3f, PlayerFsm.Singleton.TimeInCurrentState()));
        }

        var playerYVelocity = PlayerFsm.Singleton.GetYVelocity();
        if (PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.Updraft))
        {
            newYOffset = Mathf.Lerp(-3f, 5f, Mathf.InverseLerp(0, 30f, playerYVelocity));
        }
        
        if (PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.MinorLeylineStartup) || PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.MinorLeylineActive))
        {
            yLerp *= 3f;
            newYOffset = 4f;
        }
        
        if (PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.GroundMove))
        {
            var slopeOffset = Mathf.Lerp(-1f, 1f, Mathf.InverseLerp(45f, 135f, PlayerFsm.Singleton.GroundForwardSlope)) * 7.5f * PlayerFsm.Singleton.ComputeAllMovementMultipliers();
            newYOffset += slopeOffset;
        }
        
        yLerp = Mathf.Lerp(yLerp, yLerp * 5f, Mathf.InverseLerp(-5f, -15f, playerYVelocity));
        _currentYOffset = Mathf.Lerp(_currentYOffset, newYOffset, Time.deltaTime * 2f);
        pos += Vector3.up * _currentYOffset;


        _currentXZLerp = Mathf.Lerp(_currentXZLerp, xzLerp, Time.deltaTime * 15f);
        _currentYLerp = Mathf.Lerp(_currentYLerp, yLerp, Time.deltaTime * 15f);



        var newX = Mathf.Lerp(transform.position.x, pos.x, Time.deltaTime * _currentXZLerp);
        var newY = Mathf.Lerp(transform.position.y, pos.y, Time.deltaTime * _currentYLerp);
        var newZ = Mathf.Lerp(transform.position.z, pos.z, Time.deltaTime * _currentXZLerp);
        transform.position = new Vector3(newX, newY, newZ);

        // var desiredDialogueOffset = Vector3.zero;
        // if (PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.Dialogue))
        // {
        //     var controller = DialogueCanvas.Singleton.currentDialogueController;
        //     if (controller != null)
        //     {
        //         var lookAt = controller.LookAtOverride == null
        //             ? controller.transform
        //             : controller.LookAtOverride;
        //
        //         desiredDialogueOffset = (lookAt.position - transform.position) * controller.cameraFollowOffsetLerp;
        //     }
        // }
        //
        // _currentDialogueOffset = Vector3.Lerp(_currentDialogueOffset, desiredDialogueOffset, Time.deltaTime * 5f);
        // transform.position += desiredDialogueOffset;
        
        var highestPriorityZone = HighestPriorityZoneAtPosition(PlayerFsm.Singleton.transform.position);
        OnCameraFollowTriggerStay?.Invoke(highestPriorityZone);
        
    }

    public static CameraBehaviorZone HighestPriorityZoneAtPosition(Vector3 position)
    {
        var neighbors = Physics.OverlapSphere(position, 0.5f, LayerMask.GetMask("CameraBehaviorZone"),
            QueryTriggerInteraction.Collide);

        CameraBehaviorZone highestPriorityZone = null;
        
        foreach (var neighbor in neighbors)
        {
            var cameraBehaviorZone = neighbor.GetComponent<CameraBehaviorZone>();
            if (highestPriorityZone == null)
            {
                highestPriorityZone = cameraBehaviorZone;
            } else if (highestPriorityZone.priority < cameraBehaviorZone.priority)
            {
                highestPriorityZone = cameraBehaviorZone;
            }
            
        }

        return highestPriorityZone;
    }

    private void OnShaderPositionReset()
    {
        print("reset");
        Shader.SetGlobalVector("_CameraFollowWorldPosition", transform.position);
        
        Debug.DrawRay(transform.position, Vector3.up, Color.magenta, 5f);
    }
}
