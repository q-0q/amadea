using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using FMOD.Studio;
using UnityEngine;
using UnityEngine.Serialization;
using Util = Code.Misc.Util;

public class Lightbridge : MonoBehaviour
{
    public string persistentEvent = "lightbridge-";
    public string activatorEvent = "ouro-lightbridge-activator";
    private Animator _animator;
    private Interactable _interactable;
    public GameObject InteractableFx;
    public Transform BackEdge;
    
    public Transform ColliderTransform;
    private Collider _collider;
    private Renderer _colliderRenderer;

    private Collider _stairCollider;
    private Renderer _stairColliderRenderer;
    
    public GameObject Fx;

    public bool promptCamera = false;


    private CinemachineVirtualCamera _virtualCamera;
    private Transform _interactionCameraStart;
    private Transform _interactionCameraEnd;
    
    private Transform _promptCameraStart;
    private Transform _promptCameraEnd;

    private EventInstance _subAmbience;
    private EventInstance _midAmbience;
    private EventInstance _highAmbience;

    private void Awake()
    {
        _virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();

        _interactionCameraStart = transform.Find("Camera").Find("InteractionStart");
        _interactionCameraEnd = transform.Find("Camera").Find("InteractionEnd");
        
        _promptCameraStart = transform.Find("Camera").Find("PromptStart");
        _promptCameraEnd = transform.Find("Camera").Find("PromptEnd");
        
        _interactable = GetComponentInChildren<Interactable>();
        _animator = GetComponentInChildren<Animator>();
        _colliderRenderer = ColliderTransform.GetComponent<Renderer>();
        _collider = ColliderTransform.GetComponent<Collider>();
        _collider.enabled = false;
        _colliderRenderer.enabled = false;

        _stairCollider = transform.Find("StairCollider").GetComponent<Collider>();
        _stairColliderRenderer = _stairCollider.GetComponent<Renderer>();
        _stairCollider.enabled = false;
        _stairColliderRenderer.enabled = false;

        _subAmbience = FMODUnity.RuntimeManager.CreateInstance("event:/OuroSubAmbience");
        _midAmbience = FMODUnity.RuntimeManager.CreateInstance("event:/OuroMidAmbience");
        _highAmbience = FMODUnity.RuntimeManager.CreateInstance("event:/OuroHighAmbience");
        
        FMODUnity.RuntimeManager.AttachInstanceToGameObject(_subAmbience, _interactable.gameObject);
        _subAmbience.start();
        
        Fx.SetActive(false);

        if (SaveSystem.GetPersistentEventCompleted(persistentEvent))
        {
            _interactable.SetEnabled(false);
            InteractableFx.gameObject.SetActive(false);
            Util.ReplaceAnimatorTrigger(_animator, "On");
            UpdateCollider();
            _collider.enabled = true;
            Fx.SetActive(true);
            _colliderRenderer.enabled = true;
            _stairCollider.enabled = true;
            _stairColliderRenderer.enabled = true;
            FMODUnity.RuntimeManager.AttachInstanceToGameObject(_highAmbience, Fx.gameObject);
            _highAmbience.start();
        } else if (!SaveSystem.GetPersistentEventCompleted(activatorEvent))
        {
            _interactable.SetEnabled(false);
            InteractableFx.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        _interactable.OnInteracted += OnInteracted;
        LightbridgeActivator.OnLightbridgeActivatorInteracted += OnActivator;
    }

    private void OnActivator()
    {
        if (promptCamera) StartCoroutine(CameraCoroutine());
        else
        {
            _interactable.SetEnabled(true);
            InteractableFx.gameObject.SetActive(true);
            
            FMODUnity.RuntimeManager.AttachInstanceToGameObject(_midAmbience, _interactable.gameObject);
            _midAmbience.start();
        }

        IEnumerator CameraCoroutine()
        {
            CutsceneManager.Singleton.SetPseudoCutsceneActive(true);
            
            _virtualCamera.Priority = 50;

            var t = 0f;
            var d = 2f;

            _virtualCamera.transform.position = _promptCameraStart.position;
            _virtualCamera.transform.rotation = _promptCameraStart.rotation;
            
            yield return new WaitForSeconds(0.75f);
            
            while (t < d)
            {

                var w = Util.SmoothLerp01(t / d);
                
                _virtualCamera.transform.position = Vector3.Lerp(_promptCameraStart.position, _promptCameraEnd.position, w);
                _virtualCamera.transform.rotation = Quaternion.Lerp(_promptCameraStart.rotation, _promptCameraEnd.rotation, w);
                
                t += Time.deltaTime;
                yield return null;
            }
            
            _interactable.SetEnabled(true);
            InteractableFx.gameObject.SetActive(true);
            
            FMODUnity.RuntimeManager.AttachInstanceToGameObject(_midAmbience, _interactable.gameObject);
            _midAmbience.start();
            
            Util.InvokeSphereEffect(_interactable.transform.position - Vector3.up, Vector3.one * 6f, 1.25f, 0.8f, -0.5f);
            
            
            yield return new WaitForSeconds(2.5f);
            
            _virtualCamera.Priority = -50;
            CutsceneManager.Singleton.ClearPseudoCutsceneActive();
        }
    }

    private void OnDisable()
    {
        _interactable.OnInteracted -= OnInteracted;
        LightbridgeActivator.OnLightbridgeActivatorInteracted -= OnActivator;

        _subAmbience.stop(STOP_MODE.ALLOWFADEOUT);
        _midAmbience.stop(STOP_MODE.ALLOWFADEOUT);
        _highAmbience.stop(STOP_MODE.ALLOWFADEOUT);
    }

    private void OnInteracted()
    {
        Util.ReplaceAnimatorTrigger(_animator, "Channel");
        SaveSystem.WritePersistentEvent(persistentEvent);
        _interactable.SetEnabled(false);
        InteractableFx.gameObject.SetActive(false);
        FMODUnity.RuntimeManager.PlayOneShotAttached("event:/LightbridgeChannel", gameObject);

        StartCoroutine(Coroutine());
        
        IEnumerator Coroutine()
        {

            CutsceneManager.Singleton.SetPseudoCutsceneActive();
            
            _virtualCamera.Priority = 50;

            var t = 0f;
            var d = 3.25f;

            _virtualCamera.transform.position = _interactionCameraStart.position;
            _virtualCamera.transform.rotation = _interactionCameraStart.rotation;
            
            yield return new WaitForSeconds(1f);
            
            while (t < d)
            {

                var w = Util.SmoothLerp01(t / d);
                
                _virtualCamera.transform.position = Vector3.Lerp(_interactionCameraStart.position, _interactionCameraEnd.position, w);
                _virtualCamera.transform.rotation = Quaternion.Lerp(_interactionCameraStart.rotation, _interactionCameraEnd.rotation, w);
                
                t += Time.deltaTime;
                yield return null;
            }
            
            
            
            UpdateCollider();
            _collider.enabled = true;
            Fx.SetActive(true);
            Util.InvokeSphereEffect(Fx.transform.position - Vector3.up, Vector3.one * 25f, 1.25f, 0.8f, -9f);
            _colliderRenderer.enabled = true;
            
            FMODUnity.RuntimeManager.AttachInstanceToGameObject(_highAmbience, Fx.gameObject);
            _highAmbience.start();
            FMODUnity.RuntimeManager.PlayOneShotAttached("event:/LatticeComplete", Fx.gameObject);
            
            _stairCollider.enabled = true;
            _stairColliderRenderer.enabled = true;
            
            yield return new WaitForSeconds(3f);
            
            _virtualCamera.Priority = -50;
            CutsceneManager.Singleton.ClearPseudoCutsceneActive();
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void UpdateCollider()
    {
        var distance = 500f;
        var raycastOriginForwardOffset = 10f;

        var point = BackEdge.position + (transform.forward * raycastOriginForwardOffset) +
                    (transform.forward * distance);
        if (Physics.Raycast(BackEdge.position + transform.forward * raycastOriginForwardOffset, transform.forward, out var hit, distance,
                Fsm.GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
        }

        StretchBetweenPoints(ColliderTransform, BackEdge.position, point);
        _colliderRenderer.material.SetVector("_ForwardDirection", transform.forward);
        _colliderRenderer.material.SetVector("_FxPosition", Fx.transform.position);
    }
    
    public static void StretchBetweenPoints(Transform target, Vector3 point1, Vector3 point2)
    {
        // 1. Calculate the center point in world space
        Vector3 center = (point1 + point2) / 2f;

        // Convert the world center to local space if the target has a parent
        if (target.parent != null)
        {
            target.localPosition = target.parent.InverseTransformPoint(center);
        }
        else
        {
            target.localPosition = center;
        }

        // 2. Align the local Z-axis with the direction between the two points
        // (If your transform is already rotated perfectly, you can remove this line)
        target.rotation = Quaternion.LookRotation(point2 - point1);

        // 3. Calculate the distance between the points
        float distance = Vector3.Distance(point1, point2);

        // 4. Set the local scale along the Z axis
        // We divide by the parent's lossyScale.z to prevent inherited scale from distorting the math.
        float parentScaleZ = target.parent != null ? target.parent.lossyScale.z : 1f;
    
        target.localScale = new Vector3(
            target.localScale.x, 
            target.localScale.y, 
            distance / parentScaleZ
        );
    }
}
