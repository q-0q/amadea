using System;
using System.Collections;
using Code.Misc;
using UnityEngine;
using Random = UnityEngine.Random;

public class FloatTrigger : MonoBehaviour
{

    private const float RespawnDuration = 2f;
    private float _timeSinceTriggered;
    private TriggerProxy _triggerProxy;
    private Renderer _renderer;
    private Renderer _curvedStar;
    private Transform _sphereA;
    private Vector3 _baseSphereAScale;
    private CustomPointLight _flash;
    private Color _baseFlashColor;

    private const float ScaleModifer = 0.25f;

    private void Awake()
    {
        _timeSinceTriggered = 100f;
        _triggerProxy = GetComponentInChildren<TriggerProxy>();
        _renderer = transform.Find("SphereB").GetComponent<Renderer>();
        _sphereA = transform.Find("SphereA");
        _baseSphereAScale = _sphereA.localScale;
        _curvedStar = transform.Find("CurvedStar").GetComponent<Renderer>();
        _flash = transform.Find("Flash").GetComponent<CustomPointLight>();
        _baseFlashColor = _flash.Color;
    }

    private void OnEnable()
    {
        _triggerProxy.OnTriggerProxyStay += OnTrigger;
    }

    private void OnTrigger(Collider obj)
    {
        
        if (PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.Respawn) || PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.Dead) || PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.Dying)) return;
        if (PlayerFsm.Singleton.GetTimeSinceRespawn() < 2f) return;
        
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
        StartCoroutine(FlashCoroutine());
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


    private IEnumerator FlashCoroutine()
    {
        while (true)
        {
            _flash.Color = Color.black;
            yield return new WaitForSeconds(Random.Range(1.5f, 4.5f));
            var flashCount = Random.Range(2, 4);

            for (int i = 0; i < flashCount; i++)
            {
                if (_renderer.enabled) _flash.Color = _baseFlashColor;
                if (i == 0)
                {
                    yield return new WaitForSeconds(Random.Range(0.25f, 0.3f));    
                }
                else
                {
                    yield return new WaitForSeconds(Random.Range(0.05f, 0.1f));
                }
                _flash.Color = Color.black;
                yield return new WaitForSeconds(Random.Range(0.05f, 0.1f));
            }
        }
    }
}
