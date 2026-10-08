using System;
using Code.Misc;
using UnityEngine;

public class FloatTrigger : MonoBehaviour
{

    private const float RespawnDuration = 2f;
    private float _timeSinceTriggered;
    private TriggerProxy _triggerProxy;
    private Renderer _renderer;
    private Renderer _curvedStar;
    private Transform _sphereA;
    private Vector3 _baseSphereAScale;

    private const float ScaleModifer = 0.25f;

    private void Awake()
    {
        _timeSinceTriggered = 100f;
        _triggerProxy = GetComponentInChildren<TriggerProxy>();
        _renderer = transform.Find("SphereB").GetComponent<Renderer>();
        _sphereA = transform.Find("SphereA");
        _baseSphereAScale = _sphereA.localScale;
        _curvedStar = transform.Find("CurvedStar").GetComponent<Renderer>();
    }

    private void OnEnable()
    {
        _triggerProxy.OnTriggerProxyStay += OnTrigger;
    }

    private void OnTrigger(Collider obj)
    {
        Util.InvokeSphereEffect(transform.position - Vector3.up, Vector3.one * 8f, 1.25f, 0.8f, -0.5f);
        _renderer.enabled = false;
        _curvedStar.enabled = false;
        _timeSinceTriggered = 0f;
        _triggerProxy.gameObject.SetActive(false);
        PlayerFsm.Singleton.Machine.Jump(PlayerFsm.PlayerFsmState.Updraft);
        _sphereA.localScale = _baseSphereAScale * ScaleModifer;
        FMODUnity.RuntimeManager.PlayOneShotAttached("event:/LatticeComplete", gameObject);
    }

    private void OnDisable()
    {
        _triggerProxy.OnTriggerProxyStay -= OnTrigger;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _timeSinceTriggered += Time.deltaTime;

        var scaleW = Mathf.InverseLerp(RespawnDuration - 0.5f, RespawnDuration, _timeSinceTriggered);
        scaleW = Mathf.Pow(scaleW, 0.2f);
        _sphereA.localScale = Vector3.Lerp(_baseSphereAScale * ScaleModifer, _baseSphereAScale,
            scaleW);
        
        if (_timeSinceTriggered >= RespawnDuration && !_triggerProxy.gameObject.activeInHierarchy)
        {
            
            Util.InvokeSphereEffect(transform.position - Vector3.up, Vector3.one * 8f, 1.25f, 0.8f, -0.5f);
            _renderer.enabled = true;
            _curvedStar.enabled = true;
            _triggerProxy.gameObject.SetActive(true);
        }
    }
}
