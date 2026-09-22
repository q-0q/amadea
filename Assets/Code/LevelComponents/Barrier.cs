using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Code.Misc;
using UnityEngine;
using UnityEngine.Serialization;

public class Barrier : MonoBehaviour
{
    public string metaName;
    private Animator _animator;

    public Transform curtain;
    private Material _curtainMaterial;
    private Renderer _curtainRenderer;
    public Collider curtainCollider;

    public List<string> persistentEvents;
    
    private Transform _cameraFollow;
    private Transform _cameraStart;
    private Transform _cameraEnd;
    private CinemachineVirtualCamera _lightVirtualCamera;
    private Dictionary<string, GameObject> _indicators;

    private Collider _openTrigger;

    private CustomPointLight _light;
    private Color _baseLightColor;

    private Renderer _auraRenderer;


    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _curtainRenderer = curtain.GetComponent<Renderer>();
        _curtainMaterial = _curtainRenderer.material;
        _curtainMaterial.SetVector("_WorldspaceOrigin", transform.position);
        _light = GetComponentInChildren<CustomPointLight>();
        _baseLightColor = _light.Color;
        
        _cameraFollow = transform.Find("Camera").Find("CameraFollow");
        _cameraStart = transform.Find("Camera").Find("CameraFollowStart");
        _cameraEnd = transform.Find("Camera").Find("CameraFollowEnd");

        _auraRenderer = transform.Find("PlayerAuraRenderer").GetComponent<Renderer>();
        
        TryGetComponent(out _openTrigger);
        _openTrigger.enabled = false;

        if (SaveSystem.GetPersistentEventCompleted(metaName))
        {
            Util.ReplaceAnimatorTrigger(_animator, "Open");
            curtainCollider.enabled = false;
            _light.gameObject.SetActive(false);
            _auraRenderer.enabled = false;
            return;
        }
        
        _lightVirtualCamera = transform.Find("Camera").Find("MultiSwitchDoorLightVirtualCamera").GetComponentInChildren<CinemachineVirtualCamera>();
        
        var indicatorPrefab = Resources.Load("Prefab/BarrierIndicator") as GameObject;
        var indicatorHolder = transform.Find("Indicators");
        _indicators = new Dictionary<string, GameObject>();
        for (int i = 0; i < persistentEvents.Count; i++)
        {
            var position = Vector3.right * i * 0.85f;
            var obj = Instantiate(indicatorPrefab, indicatorHolder);
            obj.transform.SetLocalPositionAndRotation(position, Quaternion.identity);
            _indicators.Add(persistentEvents[i], obj);
            if (SaveSystem.GetPersistentEventCompleted(persistentEvents[i]))
            {
                var skinnedMeshRenderer = obj.GetComponentInChildren<SkinnedMeshRenderer>();
                skinnedMeshRenderer.SetBlendShapeWeight(0, 100f);
            }
        }
        
        if (IsAllSwitchesEnabled())
        {
            _openTrigger.enabled = true;
        }
        
    }

    private void OnEnable()
    {
        BarrierSwitch.OnBarrierSwitch += OnSwitch;
    }

    private void OnDisable()
    {
        BarrierSwitch.OnBarrierSwitch -= OnSwitch;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void DoOpen()
    {
        
        _curtainMaterial.SetFloat("_OpeningWeight", 0f);
        _curtainRenderer.enabled = true;
        
        Util.ReplaceAnimatorTrigger(_animator, "Opening");
        StartCoroutine(Coroutine());
        StartCoroutine(LightCoroutine());

        IEnumerator LightCoroutine()
        {
            yield return new WaitForSeconds(2f);
            var t = 0f;
            var d = 2f;
            while (t < d)
            {
                _light.Color = Color.Lerp(_baseLightColor, Color.black, t / d);
                t += Time.deltaTime;
                yield return null;
            }

            _light.gameObject.SetActive(false);
        }
        
        IEnumerator Coroutine()
        {
            
            
            var t = 0f;
            var d = 0.5f;
            while (t < d)
            {
                // var w = t > d * 0.5f ? Mathf.InverseLerp(d, d * 0.5f, t) : Mathf.InverseLerp(0f, d * 0.5f, t);
                var w = Util.SmoothLerp01(t / d);
                _curtainMaterial.SetFloat("_GlowWeight", w);
                t += Time.deltaTime;
                yield return null;
            }
            
            yield return new WaitForSeconds(0.2f);
            
            curtainCollider.enabled = false;
            t = 0f;
            d = 3.5f;
            while (t < d)
            {
                var w = Util.SmoothLerp01(t / d);
                _curtainMaterial.SetFloat("_OpeningWeight", w);
                t += Time.deltaTime;
                yield return null;
            }
            _curtainRenderer.enabled = false;
        }
    }
    
        private void OnSwitch(string metaName)
    {
        if (!persistentEvents.Contains(metaName)) return;
        StartCoroutine(MaterialCoroutine());
        StartCoroutine(CameraCoroutine());
        

        IEnumerator MaterialCoroutine()
        {
            yield return new WaitForSeconds(2f);
            float t = 0f;
            float duration = 0.5f;
            var skinnedMeshRenderer = _indicators[metaName].GetComponentInChildren<SkinnedMeshRenderer>();
            while (t < duration)
            {
                skinnedMeshRenderer.SetBlendShapeWeight(0, Util.SmoothLerp01(t / duration) * 100f);
                yield return null;
                t += Time.deltaTime;
            }
            
            // Util.InvokeSphereEffect(_lightDictionary[switchFsm].transform.position + Vector3.down, Vector3.one * 2f, 1.25f, 1f, 0f);
            FMODUnity.RuntimeManager.PlayOneShotAttached(FMODUnity.RuntimeManager.PathToEventReference("event:/MultiSwitchDoorLight"), _indicators[metaName].gameObject);
            
            yield return null;
        }
        
        IEnumerator CameraCoroutine()
        {
            CutsceneManager.Singleton.SetPseudoCutsceneActive(true);
            yield return new WaitForSeconds(1.25f);
            _cameraFollow.position = _cameraStart.position;
            float t = 0;
            float duration = 1.25f;
            _lightVirtualCamera.Priority = 20;
            yield return new WaitForSeconds(0.5f);
            while (t < duration)
            {
                _cameraFollow.position = Vector3.Lerp(_cameraStart.position, _cameraEnd.position, Util.SmoothLerp01(t/duration));
                yield return null;
                t += Time.deltaTime;
            }
            
            yield return new WaitForSeconds(0.65f);

            
            CutsceneManager.Singleton.ClearPseudoCutsceneActive();
            _lightVirtualCamera.Priority = -10;
            
            if (IsAllSwitchesEnabled())
            {
                _openTrigger.enabled = true;
                _auraRenderer.enabled = false;
            }
            
            yield return null;
        }
    }

    private bool IsAllSwitchesEnabled()
    {
        foreach (var p in persistentEvents)
        {
            if (!SaveSystem.GetPersistentEventCompleted(p)) return false;
        }
        return true;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        SaveSystem.WritePersistentEvent(metaName);
        _openTrigger.enabled = false;
        DoOpen();
    }
}
