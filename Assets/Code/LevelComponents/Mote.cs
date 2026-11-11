using System;
using System.Collections;
using Code.Misc;
using Unity.VisualScripting;
using UnityEngine;

public class Mote : MonoBehaviour
{
    private TriggerProxy OuterTriggerProxy;
    private TriggerProxy InnerTriggerProxy;

    private Renderer _rippleRenderer;
    private Renderer _rippleEdgeRenderer;
    private Renderer _curvedStarRenderer;
    
    private enum MoteState
    {
        Asleep,
        Ripple,
        Active
    }

    private MoteState _state;

    private void Awake()
    {
        transform.Find("OuterTrigger").TryGetComponent(out OuterTriggerProxy);
        transform.Find("InnerTrigger").TryGetComponent(out InnerTriggerProxy);
        transform.Find("Ripple").TryGetComponent(out _rippleRenderer);
        transform.Find("RippleEdge").TryGetComponent(out _rippleEdgeRenderer);
        transform.Find("CurvedStar").TryGetComponent(out _curvedStarRenderer);
        _state = MoteState.Asleep;
        _curvedStarRenderer.enabled = false;
    }

    private void OnEnable()
    {
        OuterTriggerProxy.OnTriggerProxyStay += OnOuterStay;
        OuterTriggerProxy.OnTriggerProxyExit += OnOuterExit;
        InnerTriggerProxy.OnTriggerProxyStay += OnInnerStay;
        InnerTriggerProxy.OnTriggerProxyExit += OnInnerExit;
    }
    
    private void OnDisable()
    {
        OuterTriggerProxy.OnTriggerProxyStay -= OnOuterStay;
        OuterTriggerProxy.OnTriggerProxyExit -= OnOuterExit;
        InnerTriggerProxy.OnTriggerProxyStay -= OnInnerStay;
        InnerTriggerProxy.OnTriggerProxyExit -= OnInnerExit;
    }
    

    private void OnInnerExit(Collider obj)
    {
        
    }

    private void OnInnerStay(Collider obj)
    {
        if (_state == MoteState.Active) return;
        _state = MoteState.Active;
        StartCoroutine(Coroutine());
        
        IEnumerator Coroutine()
        {

            var t = 0f;
            var d = 0.5f;
            
            var start = transform.position;

            while (t < d)
            {
                
                transform.position = Vector3.Lerp(transform.position, start + Vector3.up * 2f, Time.deltaTime * 3.5f);
                
                var w = Util.SmoothLerp01(t / d);
                _rippleRenderer.transform.localScale =
                    Vector3.Lerp(_rippleRenderer.transform.localScale, Vector3.one * 4f, Time.deltaTime * 3.5f);
                
                _rippleEdgeRenderer.transform.localScale =
                    Vector3.Lerp(_rippleEdgeRenderer.transform.localScale, Vector3.one * 4f, Time.deltaTime * 3.5f);
                
                _rippleEdgeRenderer.material.SetFloat("_Dot", Mathf.Lerp(_rippleEdgeRenderer.material.GetFloat("_Dot"), 50f, Time.deltaTime * 2f));
                _rippleRenderer.material.SetFloat("_Alpha", Mathf.Lerp(_rippleRenderer.material.GetFloat("_Alpha"), 0f, Time.deltaTime * 6f));
                t += Time.deltaTime;
                yield return null;
            }
            
            _curvedStarRenderer.enabled = true;
            
            t = 0f;
            d = 0.5f;


            
            while (t < d)
            {
                var w = Util.SmoothLerp01(t / d);

                
                
                _rippleRenderer.transform.localScale =
                    Vector3.Lerp(_rippleRenderer.transform.localScale, Vector3.one * 3f, Time.deltaTime * 3.5f);
                
                _rippleEdgeRenderer.transform.localScale =
                    Vector3.Lerp(_rippleEdgeRenderer.transform.localScale, Vector3.one * 6f, Time.deltaTime * 3.5f);
                
                _rippleEdgeRenderer.material.SetFloat("_Dot", Mathf.Lerp(_rippleEdgeRenderer.material.GetFloat("_Dot"), 3f, Time.deltaTime * 10f));
                // _rippleEdgeRenderer.material.SetFloat("_Alpha", Mathf.Lerp(_rippleEdgeRenderer.material.GetFloat("_Alpha"), 3f, Time.deltaTime * 2f));
                t += Time.deltaTime;
                yield return null;
            }
            
            // _rippleEdgeRenderer.enabled = false;
            _rippleRenderer.enabled = false;
            
            
            t = 0f;
            d = 1f;

            start = transform.position;

            while (t < d)
            {
                var w = Util.SmoothLerp01(t / d);

                _curvedStarRenderer.transform.position = Util.LerpWithArc(start, PlayerFsm.Singleton.transform.position + Vector3.up * 2f, w, 2f, Vector3.up);
                t += Time.deltaTime;
                yield return null;
            }
            
            _curvedStarRenderer.enabled = false;
            
            Util.InvokeSphereEffect(_curvedStarRenderer.transform.position - Vector3.up, Vector3.one * 6f, 1.25f, 0.8f, -1f);
        }
    }

    private void OnOuterExit(Collider obj)
    {
        if (_state == MoteState.Ripple) _state = MoteState.Asleep;
    }

    private void OnOuterStay(Collider obj)
    {
        if (_state == MoteState.Asleep) _state = MoteState.Ripple;
    }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        print(_state);
        
        if (_state == MoteState.Asleep)
        {
            _rippleRenderer.material.SetFloat("_Alpha", Mathf.Lerp(_rippleRenderer.material.GetFloat("_Alpha"), 0f, Time.deltaTime * 5f));
            _rippleRenderer.material.SetFloat("_Refraction", Mathf.Lerp(_rippleRenderer.material.GetFloat("_Refraction"), 0f, Time.deltaTime * 5f));
            _rippleEdgeRenderer.material.SetFloat("_Alpha", Mathf.Lerp(_rippleEdgeRenderer.material.GetFloat("_Alpha"), 0f, Time.deltaTime * 3f));
        }
        
        if (_state == MoteState.Ripple)
        {
            _rippleRenderer.material.SetFloat("_Alpha", Mathf.Lerp(_rippleRenderer.material.GetFloat("_Alpha"), 1f, Time.deltaTime * 3f));
            _rippleRenderer.material.SetFloat("_Refraction", Mathf.Lerp(_rippleRenderer.material.GetFloat("_Refraction"), 1f, Time.deltaTime * 3f));
            _rippleEdgeRenderer.material.SetFloat("_Alpha", Mathf.Lerp(_rippleEdgeRenderer.material.GetFloat("_Alpha"), 1f, Time.deltaTime * 2f));
        }
        
                
        if (_state == MoteState.Active)
        {
            
        }
        
        
    }
}
