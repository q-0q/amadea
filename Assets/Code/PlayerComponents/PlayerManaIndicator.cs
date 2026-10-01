using System;
using System.Collections;
using Code.Misc;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerManaIndicator : MonoBehaviour
{

    private Renderer _indicatorRenderer;
    private Renderer _hullRenderer;
    private float _maxManaTime;
    private float _consumedManaTime;

    private void Awake()
    {
        _hullRenderer = transform.Find("Hull").GetComponent<Renderer>();
        _indicatorRenderer = transform.Find("Indicator").GetComponent<Renderer>();
        
        _hullRenderer.material.SetFloat("_Alpha", 0);
        _indicatorRenderer.material.SetFloat("_Alpha", 0);
        _maxManaTime = -10f;
        _consumedManaTime = -100f;
    }

    private void OnEnable()
    {
        PlayerManaManager.OnPlayerConsumedMana += OnPlayerConsumedMana;
        PlayerManaManager.OnPlayerMaxMana += OnPlayerMaxMana;
    }

    private void OnDisable()
    {
        PlayerManaManager.OnPlayerConsumedMana -= OnPlayerConsumedMana;
        PlayerManaManager.OnPlayerMaxMana -= OnPlayerMaxMana;
    }
    
    private void OnPlayerMaxMana()
    {
        _maxManaTime = Time.time;
    }
    
    private void OnPlayerConsumedMana()
    {
        _consumedManaTime = Time.time;
    }

    public void Consume()
    {
        _indicatorRenderer.enabled = false;
        Vibrate();
        StartCoroutine(Coroutine());

        IEnumerator Coroutine()
        {
            var t = 0f;
            var d = 0.2f;
            while (t < d)
            {
                var w = Util.SmoothLerp01(t / d);
                _hullRenderer.material.SetFloat("_Weight_1", 1f - w);
                t += Time.deltaTime;
                yield return null;
            }
        }
    }

    private void Vibrate()
    {
        transform.DOComplete();
        transform.DOPunchPosition(new Vector3(0, 0.05f, 0), 0.3f, 20);
    }

    public void Replenish()
    {
        _indicatorRenderer.enabled = true;
        Vibrate();

        StartCoroutine(Coroutine());
        
        // FMODUnity.RuntimeManager.PlayOneShotAttached("event:/ManaRegen", PlayerFsm.Singleton.gameObject);

        IEnumerator Coroutine()
        {
            var t = 0f;
            var d = 0.2f;
            while (t < d)
            {
                var w = Util.SmoothLerp01(t / d);
                _hullRenderer.material.SetFloat("_Weight_1", 1f - w);
                t += Time.deltaTime;
                yield return null;
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (_consumedManaTime >= _maxManaTime)
        {
            _hullRenderer.material.SetFloat("_Alpha", 1f);
            _indicatorRenderer.material.SetFloat("_Alpha", 1f);
        }
        else
        {
            var a = 1f - Mathf.InverseLerp(_maxManaTime + 0.8f, _maxManaTime + 0.9f, Time.time);
            _hullRenderer.material.SetFloat("_Alpha", a);
            _indicatorRenderer.material.SetFloat("_Alpha", a);
        }
    }
}
