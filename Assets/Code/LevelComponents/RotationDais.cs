using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class RotationDais : MonoBehaviour
{

    private Interactable _interactable;
    private bool _active;
    private Transform _rotator;
    private Transform _rotatorBob;
    public Transform _ball;
    private PlayerInput _playerInput;

    private Vector3 _baseBallWorldPosition;
    private Vector3 _baseRotatorBobWorldPosition;

    private string _baseInteractableText;

    private bool _setupComplete;

    public Transform _childToRotator;

    public bool bob = true;

    // --- NEW AXIS RESTRICTION FIELDS ---
    public bool restrictAxis = false;
    public Vector3 allowedAxis = Vector3.up;

    private void Awake()
    {
        _interactable = GetComponentInChildren<Interactable>();
        _playerInput = GetComponent<PlayerInput>();
        _rotatorBob = transform.Find("RotatorBob");
        _rotator = _rotatorBob.Find("Rotator");
        _baseInteractableText = _interactable.text;
        _baseBallWorldPosition = _ball.position;
        _baseRotatorBobWorldPosition = _rotator.position;
        if (_childToRotator != null)_childToRotator.SetParent(_rotator);
        _setupComplete = false;
    }

    private void OnEnable()
    {
        _interactable.OnInteracted += OnInteracted;
    }

    private void OnDisable()
    {
        _interactable.OnInteracted -= OnInteracted;
    }

    private void OnInteracted()
    {
        if (_active)
        {
            PlayerFsm.Singleton.Machine.Jump(PlayerFsm.PlayerFsmState.Idle);
            _active = false;
            _interactable.text = _baseInteractableText;
            Vibrate();

            foreach (var post in GetComponentsInChildren<RotationDaisPost>())
            {
                post.Deactivate();
            }

            _setupComplete = false;
            return;
        }
        
        PlayerFsm.Singleton.Machine.Jump(PlayerFsm.PlayerFsmState.WalkToRotationDaisPosition);
        PlayerFsm.Singleton.walkToPositionTarget = _interactable.transform.position;
        PlayerFsm.Singleton.walkToPositionArrivalDistanceModifier = _interactable.arrivalDistanceModifier;
        

        
        _active = true;
    }

    private void Vibrate()
    {
        _rotator.DOShakePosition(0.4f, 0.3f, 20);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    
    private Vector3 _currentRotationVelocity = Vector3.zero;

    void Update()
    {
        Vector2 input = _active && PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.RotationDaisInteract) 
            ? _playerInput.actions["Move"].ReadValue<Vector2>() : Vector3.zero;

        if (_active && PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.RotationDaisInteract) &&
            !_setupComplete)
        {
            foreach (var post in GetComponentsInChildren<RotationDaisPost>())
            {
                post.Activate();
            }
            _interactable.text = "Leave";
            _rotator.DOComplete();
            Vibrate();
            _setupComplete = true;
        }
        
        Vector3 right = Camera.main.transform.right;
        Vector3 up = transform.up; 
        Vector3 targetAxis = (-up * input.x) + (right * input.y);
        
        // --- AXIS RESTRICTION CHECK ---
        if (restrictAxis && allowedAxis != Vector3.zero)
        {
            // Squash the input rotation target onto the allowed axis only
            targetAxis = Vector3.Project(targetAxis, allowedAxis.normalized);
        }
        
        float acceleration = 1.5f;
        float maxSpeed = 100f;
        
        _currentRotationVelocity = Vector3.MoveTowards(
            _currentRotationVelocity, 
            targetAxis, 
            acceleration * Time.deltaTime
        );
        
        _rotator.Rotate(_currentRotationVelocity * (maxSpeed * Time.deltaTime), Space.World);
        _ball.Rotate(_currentRotationVelocity * (maxSpeed * Time.deltaTime * 3f), Space.World);
        
        Vector3 ballWorldSpaceOffset = new Vector3(0f, Mathf.Sin(3f * Time.time) * 0.1f, 0f);
        _ball.position = _baseBallWorldPosition + ballWorldSpaceOffset;
        
        Vector3 rotatorBobWorldspaceOffset = new Vector3(0f, Mathf.Sin(1.5f * Time.time) * 0.75f * (bob ? 1f : 0f), 0f);
        _rotatorBob.position = _baseRotatorBobWorldPosition + rotatorBobWorldspaceOffset;
        
    }
}