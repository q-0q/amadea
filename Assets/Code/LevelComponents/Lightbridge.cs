using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Code.Misc;
using UnityEngine;
using UnityEngine.Serialization;

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


    private CinemachineVirtualCamera _virtualCamera;
    private Transform _cameraStart;
    private Transform _cameraEnd;

    private void Awake()
    {
        _virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();

        _cameraStart = transform.Find("Camera").Find("Start");
        _cameraEnd = transform.Find("Camera").Find("End");
        
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
        _interactable.SetEnabled(true);
        InteractableFx.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        _interactable.OnInteracted -= OnInteracted;
        LightbridgeActivator.OnLightbridgeActivatorInteracted -= OnActivator;
    }

    private void OnInteracted()
    {
        Util.ReplaceAnimatorTrigger(_animator, "Channel");
        SaveSystem.WritePersistentEvent(persistentEvent);
        _interactable.SetEnabled(false);
        InteractableFx.gameObject.SetActive(false);

        StartCoroutine(Coroutine());
        
        IEnumerator Coroutine()
        {

            CutsceneManager.Singleton.SetPseudoCutsceneActive();
            
            _virtualCamera.Priority = 50;

            var t = 0f;
            var d = 3.25f;

            _virtualCamera.transform.position = _cameraStart.position;
            _virtualCamera.transform.rotation = _cameraStart.rotation;
            
            yield return new WaitForSeconds(1f);
            
            while (t < d)
            {

                var w = Util.SmoothLerp01(t / d);
                
                _virtualCamera.transform.position = Vector3.Lerp(_cameraStart.position, _cameraEnd.position, w);
                _virtualCamera.transform.rotation = Quaternion.Lerp(_cameraStart.rotation, _cameraEnd.rotation, w);
                
                t += Time.deltaTime;
                yield return null;
            }
            
            
            
            UpdateCollider();
            _collider.enabled = true;
            Fx.SetActive(true);
            Util.InvokeSphereEffect(Fx.transform.position - Vector3.up, Vector3.one * 25f, 1.25f, 0.8f, -9f);
            _colliderRenderer.enabled = true;
            
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
