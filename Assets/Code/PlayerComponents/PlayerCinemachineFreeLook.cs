using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Code.Fsm.TrialCollectibleFSM;
using Code.Misc;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCinemachineFreeLook : MonoBehaviour
{
    public static PlayerCinemachineFreeLook Singleton;
    private CinemachineFreeLook _freeLook;
    private PlayerInput _playerInput;
    private float _baseXSpeed;
    private float _baseYSpeed;
    private float _timeSincePlayerLookInput;
    
    private float _rampUpTime;
    
    

    private bool _scriptActive;
    private Vector3 _scriptTargetDirection;
    private float _scriptDuration;
    private float _timeInScript;

    private bool _isAutocamEnabled;

    private bool _settingsMenuOpen;

    private CinemachineBrain _brain;

    private float _baseFov;
    private bool _preventYRecenter;
    private Vector3 _desiredOffset;
    private CinemachineCameraOffset _cinemachineOffset;

    void Awake()
    {
        Singleton = this;
        TryGetComponent(out _freeLook);
        _isAutocamEnabled = true;
        _baseXSpeed = _freeLook.m_XAxis.m_MaxSpeed;
        _baseYSpeed = _freeLook.m_YAxis.m_MaxSpeed;
        _settingsMenuOpen = false;
        _brain = FindObjectOfType<CinemachineBrain>();
        _baseFov = _freeLook.m_Lens.FieldOfView;
        _freeLook.m_YAxis.Value = 0.55f;
        _freeLook.m_YAxis.m_InputAxisValue = 0f;
        _freeLook.m_XAxis.m_InputAxisValue = 0f;
        _preventYRecenter = false;
        _cinemachineOffset = GetComponent<CinemachineCameraOffset>();
        _desiredOffset = Vector3.zero;
        
        OnMetaSaveDataUpdated(MetaSaveSystem.LoadCachedMetaSaveData());
    }

    private void Start()
    {
        PlayerFsm.Singleton.gameObject.TryGetComponent(out _playerInput);
        _timeSincePlayerLookInput = 0f;
        _rampUpTime = 8f;
        
        
        var xAngle = Vector3.SignedAngle(Vector3.forward, PlayerFsm.Singleton.transform.forward, Vector3.up);
        _freeLook.m_XAxis.Value = xAngle;
        
        var highestPriorityZone = CameraFollow.HighestPriorityZoneAtPosition(PlayerFsm.Singleton.transform.position);
        if (highestPriorityZone != null)
        {
            highestPriorityZone.GetCameraForward(PlayerFsm.Singleton.transform.position, out var y);
            _freeLook.m_YAxis.Value = y;
        }
        
        var saveData = SaveSystem.LoadCachedSaveData();
        var positionIdTransform = Util.FindGamePositionById(saveData.playerInGamePositionId, out var cameraRotationOffset);
        AddXAxisOffset(cameraRotationOffset);
    }

    private void Update()
    {
        if (_scriptActive || _brain.ActiveVirtualCamera != _freeLook || GameMenu.Singleton.IsMenuOpen() || !InventoryMenuFsm.Singleton.Machine.IsInState(InventoryMenuFsm.InventoryMenuFsmState.Closed) || PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.TrialTeleport) || CutsceneManager.Singleton.IsCutsceneCameraDisabled() || PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.UseMap))
        {
            _freeLook.m_XAxis.m_InputAxisValue = 0;
            _freeLook.m_XAxis.Reset();
            _freeLook.m_YAxis.m_InputAxisValue = 0;
            _freeLook.m_YAxis.Reset();
            return;
        }
        
        HandleCollision();
        
        var lookVector2 = _playerInput.actions["Look"].ReadValue<Vector2>() * (4f * Time.timeScale * Time.deltaTime);
        _timeSincePlayerLookInput += Time.deltaTime;

        if (_timeSincePlayerLookInput >= 2.5f && !_preventYRecenter && DialogueCanvas.Singleton.TimeSinceDialogueClosed > 2.5f)
        {
            var y = 0.55f;
            if (DialogueCanvas.Singleton.currentDialogueController != null)
            {
                y = DialogueCanvas.Singleton.currentDialogueController.CameraY;
            }
            
            _freeLook.m_YAxis.Value = Mathf.Lerp(_freeLook.m_YAxis.Value, y, Time.deltaTime * Mathf.Lerp(0.25f, 2f, Mathf.InverseLerp(2.5f, 3.5f, _timeSincePlayerLookInput)));
        }
        if (lookVector2.magnitude < 0.01f)
        {
            _freeLook.m_XAxis.m_InputAxisValue = 0;
            _freeLook.m_YAxis.m_InputAxisValue = 0;
            return;
        }
        _timeSincePlayerLookInput = 0f;

        if (InputTypeManager.Singleton.GetCurrentInputType() == InputTypeManager.InputType.Pad) lookVector2 *= 5f;
        _freeLook.m_XAxis.m_InputAxisValue = lookVector2.x;
        _freeLook.m_YAxis.m_InputAxisValue = lookVector2.y;
        

    }

    private void OnEnable()
    {
        MetaSaveSystem.OnMetaSaveDataUpdated += OnMetaSaveDataUpdated;
        TestCutsceneFsm.OnIntroCutsceneGondolaTeleported += OnWarp;
        PlayerFsm.OnPlayerTeleported += OnWarp;
    }

    private void OnDisable()
    {
        MetaSaveSystem.OnMetaSaveDataUpdated -= OnMetaSaveDataUpdated;
        TestCutsceneFsm.OnIntroCutsceneGondolaTeleported -= OnWarp;
        PlayerFsm.OnPlayerTeleported -= OnWarp;
    }
    
    private void OnMetaSaveDataUpdated(MetaSaveSystem.MetaSaveData metaSaveData)
    {
        _isAutocamEnabled = metaSaveData.autoCamEnabled;
        _freeLook.m_XAxis.m_MaxSpeed = _baseXSpeed * metaSaveData.cameraSensitivityModifier * 0.1f;
        _freeLook.m_YAxis.m_MaxSpeed = _baseYSpeed * metaSaveData.cameraSensitivityModifier * 0.1f;

        switch (metaSaveData.cameraUpdateMode)
        {
            case "SmartUpdate":
            {
                _brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.SmartUpdate;
                break;
            }
            
            case "FixedUpdate":
            {
                _brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.FixedUpdate;
                break;
            }
            
            case "LateUpdate":
            {
                _brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.LateUpdate;
                break;
            }
        }
    }

    private void OnWarp(Vector3 delta)
    {
        // StartCoroutine(Coroutine());
        CinemachineCore.Instance.OnTargetObjectWarped(_freeLook.Follow, delta);
        // IEnumerator Coroutine()
        // {
        //     for (int i = 0; i < 6; i++)
        //     {
        //         yield return null;
        //     }
        // }
    }

    // private void OnSettingsMenuOpened()
    // {
    //     _settingsMenuOpen = true;
    // }
    //
    // private void OnSettingsMenuClosed()
    // {
    //     _settingsMenuOpen = false;
    // }
    //

    
    
    public void AddXAxisOffset(float value)
    {
        _freeLook.m_XAxis.Value += value;
    }
    

    public void OnPlayerCinemachineFreeLookScript(Vector3 direction, float duration, float y = 0.7f, float linger = 0f)
    {
        if (_scriptActive) return;
        StartCoroutine(InvokeScript(direction, duration));
        IEnumerator InvokeScript(Vector3 direction, float duration)
        {
            direction = new Vector3(direction.x, 0f, direction.z);
            var xAngle = Vector3.SignedAngle(Vector3.forward, direction, transform.up);
            var newXQuat = Quaternion.Euler(0, xAngle, 0);
            var oldXQuat = Quaternion.Euler(0, _freeLook.m_XAxis.Value, 0);

            var oldY = _freeLook.m_YAxis.Value;
            
            _scriptActive = true;
            
            float t = 0;
            while (t < duration)
            {
                var w = Util.SmoothLerp01( t / duration);
                _freeLook.m_XAxis.Value = Quaternion.Lerp(oldXQuat, newXQuat, w).eulerAngles.y;
                _freeLook.m_YAxis.Value = Mathf.Lerp(oldY, y, w);
                t += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(linger);
            
            _scriptActive = false;  
        }
    }

    public void SetAxes(float xValue, float yValue)
    {
        _freeLook.m_XAxis.Value = xValue;
        _freeLook.m_YAxis.Value = yValue;

    }

    public CinemachineFreeLook GetFreeLook()
    {
        return _freeLook;
    }

    public float GetBaseFov()
    {
        return _baseFov;
    }

    public IEnumerator PreventYRecenterForDuration(float duration)
    {
        _preventYRecenter = true;
        yield return new WaitForSeconds(duration);
        _preventYRecenter = false;
    }

    public void SetDesiredOffset(Vector3 offset)
    {
        _desiredOffset = offset;
    }
    private void HandleCollision()
    {

        if (PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.Travel))
        {
            _cinemachineOffset.m_Offset = _desiredOffset;
            return;
        }
        
        var padding = 2f;
        var desiredPosition = transform.position + _desiredOffset;
        var toCamera = desiredPosition - PlayerFsm.Singleton.transform.position;
        if (Physics.Raycast(PlayerFsm.Singleton.transform.position, toCamera, out var toCameraHit, toCamera.magnitude + padding,
                Fsm.GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore))
        {

            if (Physics.Raycast(desiredPosition, -toCamera, out var toPlayerHit, toCamera.magnitude - toCameraHit.distance,
                    Fsm.GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore))
            {
                _cinemachineOffset.m_Offset = _desiredOffset;
            }
            else
            {
                _cinemachineOffset.m_Offset = new Vector3(_desiredOffset.x, _desiredOffset.y, toCamera.magnitude - toCameraHit.distance + padding);
            }
        }
        else
        {
            _cinemachineOffset.m_Offset = _desiredOffset;
        }
    }

    
}
