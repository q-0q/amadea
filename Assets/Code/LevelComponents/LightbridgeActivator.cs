using System;
using System.Collections;
using Cinemachine;
using Code.Misc;
using UnityEngine;

public class LightbridgeActivator : MonoBehaviour
{

    private Interactable _interactable;
    public static event Action OnLightbridgeActivatorInteracted;
    public string persistentEvent = "ouro-lightbridge-activator";
    private CinemachineVirtualCamera _virtualCamera;

    private Transform _cameraStart;
    private Transform _cameraEnd;

    public Transform Fx;

    private void Awake()
    {
        _interactable = GetComponentInChildren<Interactable>();
        _virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
        _cameraStart = transform.Find("CameraStart");
        _cameraEnd = transform.Find("CameraEnd");

        if (SaveSystem.GetPersistentEventCompleted(persistentEvent))
        {
            Fx.gameObject.SetActive(true);
            _interactable.SetEnabled(false);
        }
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
        // _interactable.SetEnabled(false);
        StartCoroutine(Coroutine());

        IEnumerator Coroutine()
        {
            Fx.gameObject.SetActive(false);
            _interactable.SetEnabled(false);
            SaveSystem.WritePersistentEvent(persistentEvent);
            CutsceneManager.Singleton.SetPseudoCutsceneActive();
            yield return new WaitForSeconds(0.5f);
            _virtualCamera.transform.position = _cameraStart.position;
            _virtualCamera.transform.rotation = _cameraStart.rotation;
            _virtualCamera.Priority = 50;
            
            yield return new WaitForSeconds(0.5f);

            var t = 0f;
            var d = 1.5f;

            while (t < d)
            {
                var w = Util.SmoothLerp01(t / d);
                _virtualCamera.transform.position = Vector3.Slerp(_cameraStart.position, _cameraEnd.position, w);
                _virtualCamera.transform.rotation = Quaternion.Slerp(_cameraStart.rotation, _cameraEnd.rotation, w);
                
                t += Time.deltaTime;
                yield return null;
            }
            
            yield return new WaitForSeconds(0.5f);
            
            Util.InvokeSphereEffect(Fx.position - Vector3.up, Vector3.one * 25f, 1.25f, 0.8f, -9f);
            Fx.gameObject.SetActive(true);
            
            yield return new WaitForSeconds(3f);
            
            _virtualCamera.Priority = -50;
            CutsceneManager.Singleton.ClearPseudoCutsceneActive();
        }
        
        OnLightbridgeActivatorInteracted?.Invoke();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
