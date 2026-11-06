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
    
    private bool _isGravityGunCoroutineActive;

    private void Start()
    {
        transform.position = PlayerFsm.Singleton.transform.position;
        transform.rotation = PlayerFsm.Singleton.transform.rotation;
        _currentXZLerp = 100f;
        _currentYLerp = YLerpRate;
        Shader.SetGlobalVector("_CameraFollowWorldPosition", transform.position);
        _brain = Camera.main.GetComponent<CinemachineBrain>();
        _freeLook = PlayerCinemachineFreeLook.Singleton.GetFreeLook();
        _isGravityGunCoroutineActive = false;
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
        PlayerFsm.OnPlayerUpdateFinished += OnPlayerUpdateFinished;
        GravityGun.OnGravityGunUsed += OnGravityGun;
    }

    private void OnGravityGun()
    {

        StartCoroutine(Coroutine());
        
        IEnumerator Coroutine()
        {
            _isGravityGunCoroutineActive = true;
            var t = 0f;
            var d = 1.25f;
            while (t < d)
            {
                t += Time.deltaTime;
                yield return null;
            }
            _isGravityGunCoroutineActive = false;
        }
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
        PlayerFsm.OnPlayerUpdateFinished -= OnPlayerUpdateFinished;
        GravityGun.OnGravityGunUsed -= OnGravityGun;
    }

    private void OnWarp(Vector3 delta)
    {
        transform.position += delta;
        Shader.SetGlobalVector("_CameraFollowWorldPosition", transform.position);
    }
    
private void OnPlayerUpdateFinished()
{
    
    Vector3 up = PlayerFsm.Singleton.GetUpVector();

    if (_isGravityGunCoroutineActive)
    {
        var currentShaderValue = Shader.GetGlobalVector("_CameraFollowWorldPosition");
        Shader.SetGlobalVector("_CameraFollowWorldPosition", Vector3.Lerp(currentShaderValue, transform.position, Time.deltaTime * 2.5f));
    }
    else if (CutsceneManager.Singleton.IsCutsceneOverwriteCameraFollowShaderPosition(out var t) && _freeLook.gameObject != _brain.ActiveVirtualCamera.VirtualCameraGameObject)
    {
        Shader.SetGlobalVector("_CameraFollowWorldPosition", t == null ? Camera.main.transform.position : t.position);
    }
    else
    {
        var shaderPositionXZLerpSpeed = 10f;
        var shaderPositionYLerpSpeed =
            Mathf.Lerp(1.75f, 10f, Mathf.InverseLerp(-15f, -30f, PlayerFsm.Singleton.GetYVelocity()));
        
        var currentShaderValue = Shader.GetGlobalVector("_CameraFollowWorldPosition");
        
        // Decompose shader position into custom up height and plane position
        float currentShaderHeight = Vector3.Dot(currentShaderValue, up);
        float targetShaderHeight = Vector3.Dot(transform.position, up);
        float newShaderHeight = Mathf.Lerp(currentShaderHeight, targetShaderHeight, Time.deltaTime * shaderPositionYLerpSpeed);
    
        Vector3 currentShaderPlane = new Vector3(currentShaderValue.x, currentShaderValue.y, currentShaderValue.z) - up * currentShaderHeight;
        Vector3 targetShaderPlane = transform.position - up * targetShaderHeight;
        Vector3 newShaderPlane = Vector3.Lerp(currentShaderPlane, targetShaderPlane, Time.deltaTime * shaderPositionXZLerpSpeed);
    
        Shader.SetGlobalVector("_CameraFollowWorldPosition", newShaderPlane + up * newShaderHeight);
    }

    var pos = CameraFollowTarget.Singleton.transform.position;
    var yLerp = PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.CutsceneWary)
        ? YLerpRate / 4f
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
        yLerp *= 2f;
        newYOffset = Mathf.Lerp(-4f, 0f, Mathf.InverseLerp(-30f, 10f, playerYVelocity));
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
    
    if (_isGravityGunCoroutineActive)
    {
        newYOffset = -2f;
    }

    yLerp = Mathf.Lerp(yLerp, yLerp * 10f, Mathf.InverseLerp(-5f, -15f, playerYVelocity));
    _currentYOffset = Mathf.Lerp(_currentYOffset, newYOffset, Time.deltaTime * 2f);
    
    // Apply offset along the custom up vector instead of Vector3.up
    pos += up * _currentYOffset;

    _currentXZLerp = Mathf.Lerp(_currentXZLerp, xzLerp, Time.deltaTime * 15f);
    _currentYLerp = Mathf.Lerp(_currentYLerp, yLerp, Time.deltaTime * 15f);

    // Decompose position interpolation into custom up height and plane position
    float currentHeight = Vector3.Dot(transform.position, up);
    float targetHeight = Vector3.Dot(pos, up);
    float newHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * _currentYLerp);

    Vector3 currentPlanePos = transform.position - up * currentHeight;
    Vector3 targetPlanePos = pos - up * targetHeight;
    Vector3 newPlanePos = Vector3.Lerp(currentPlanePos, targetPlanePos, Time.deltaTime * _currentXZLerp);

    transform.position = newPlanePos + up * newHeight;

    // Keep rotation aligned with the custom up vector
    Vector3 forward = Vector3.ProjectOnPlane(Vector3.forward, up);
    if (forward.sqrMagnitude < 0.0001f) forward = Vector3.ProjectOnPlane(transform.forward, up);
    transform.up = Vector3.Lerp(transform.up, up, Time.deltaTime * 5f);

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
